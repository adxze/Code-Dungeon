using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using System.Linq;


public class CodeGameController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshPro input field used for editing code.")]
    public TMP_InputField codeInput;
    [Tooltip("Non-editable TextMeshProUGUI used to show highlighted code behind the input field.")]
    public TextMeshProUGUI highlightedText;
    [Tooltip("Displays error and feedback messages after code execution.")]
    public TextMeshProUGUI feedbackText;
    
    [Header("Execution Settings")]
    [Tooltip("Time between commands. Adjust to speed up or slow down execution.")]
    public float commandDelay = 0.1f;
    [Tooltip("Maximum iterations for while loops (safety limit).")]
    public int maxWhileIterations = 1000;

    // Command signature
    public delegate IEnumerator CommandHandler(string[] args);

    // Core interpreter state
    private readonly Dictionary<string, CommandHandler> commandRegistry = new();
    private readonly Dictionary<string, object> variables = new();
    private readonly List<string> feedback = new();
    private bool isRunning;
    private int currentLine = 0; // For error reporting
    

    // Statement base class
    private abstract class Statement
    {
        public int lineNumber;
        public abstract IEnumerator Execute(CodeGameController controller);
    }

    // Function call
    private class FunctionCall : Statement
    {
        public string name;
        public string[] args;

        public override IEnumerator Execute(CodeGameController controller)
        {
            if (!controller.commandRegistry.ContainsKey(name))
            {
                controller.AddFeedback($"Line {lineNumber}: Unknown function '{name}'");
                yield break;
            }

            // Evaluate all arguments
            string[] evaluatedArgs = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                var result = controller.EvaluateExpression(args[i]);
                evaluatedArgs[i] = result?.ToString() ?? "";
            }

            var handler = controller.commandRegistry[name];
            yield return controller.StartCoroutine(handler(evaluatedArgs));
        }
    }

    // Variable assignment
    private class Assignment : Statement
    {
        public string varName;
        public string expression;

        public override IEnumerator Execute(CodeGameController controller)
        {
            var value = controller.EvaluateExpression(expression);
            controller.variables[varName] = value;
            yield break;
        }
    }

    // If/elif/else statement
    private class IfStatement : Statement
    {
        public List<(string condition, List<Statement> block)> branches; // if and elif branches
        public List<Statement> elseBlock;

        public override IEnumerator Execute(CodeGameController controller)
        {
            // Check each branch condition
            foreach (var (condition, block) in branches)
            {
                if (controller.EvaluateBool(condition))
                {
                    foreach (var stmt in block)
                    {
                        yield return controller.StartCoroutine(stmt.Execute(controller));
                        yield return new WaitForSeconds(controller.commandDelay);
                    }
                    yield break;
                }
            }

            // If no branch was taken, execute else block
            if (elseBlock != null)
            {
                foreach (var stmt in elseBlock)
                {
                    yield return controller.StartCoroutine(stmt.Execute(controller));
                    yield return new WaitForSeconds(controller.commandDelay);
                }
            }
        }
    }

    // While loop
    private class WhileLoop : Statement
    {
        public string condition;
        public List<Statement> body;

        public override IEnumerator Execute(CodeGameController controller)
        {
            int iterations = 0;
            while (controller.EvaluateBool(condition))
            {
                if (++iterations > controller.maxWhileIterations)
                {
                    controller.AddFeedback($"Line {lineNumber}: While loop exceeded {controller.maxWhileIterations} iterations");
                    break;
                }

                foreach (var stmt in body)
                {
                    yield return controller.StartCoroutine(stmt.Execute(controller));
                    yield return new WaitForSeconds(controller.commandDelay);
                }
                yield return null; // Allow frame update
            }
        }
    }

    // For loop with range
    private class ForLoop : Statement
    {
        public string iterator;
        public string rangeExpr;
        public List<Statement> body;

        public override IEnumerator Execute(CodeGameController controller)
        {
            var range = controller.ParseRange(rangeExpr);
            if (range == null)
            {
                controller.AddFeedback($"Line {lineNumber}: Invalid range expression");
                yield break;
            }

            // Execute loop
            foreach (int value in range)
            {
                controller.variables[iterator] = value;
                foreach (var stmt in body)
                {
                    yield return controller.StartCoroutine(stmt.Execute(controller));
                    yield return new WaitForSeconds(controller.commandDelay);
                }
            }

            controller.variables.Remove(iterator);
        }
    }

    private void Start()
    {
        if (codeInput != null && highlightedText != null)
        {
            codeInput.onValueChanged.AddListener(OnCodeChanged);
        }
    }

    public void RegisterCommand(string name, CommandHandler handler)
    {
        if (string.IsNullOrWhiteSpace(name) || handler == null) return;
        commandRegistry[name] = handler;
    }

    public void ClearCommands()
    {
        commandRegistry.Clear();
    }

    public void RunCode()
    {
        if (isRunning) return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "Game finished. Commands are disabled.";
            }
            return;
        }

        if (RunLimitManager.Instance != null && !RunLimitManager.Instance.TryRegisterRun())
        {
            return;
        }
        
        variables.Clear();
        feedback.Clear();
        currentLine = 0;

        string code = codeInput != null ? codeInput.text : "";
        string[] lines = code.Split(new[] { '\r', '\n' }, System.StringSplitOptions.None);
        
        int index = 0;
        var statements = ParseBlock(lines, ref index, 0);
        StartCoroutine(ExecuteStatements(statements));
    }

    private List<Statement> ParseBlock(string[] lines, ref int index, int expectedIndent)
    {
        var statements = new List<Statement>();

        while (index < lines.Length)
        {
            string line = lines[index];
            int indent = GetIndentation(line);
            string trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
            {
                index++;
                continue;
            }

            if (indent < expectedIndent)
            {
                break;
            }
            else if (indent > expectedIndent)
            {
                AddFeedback($"Line {index + 1}: Unexpected indentation");
                index++;
                continue;
            }

            int lineNum = index + 1;

            if (trimmed.StartsWith("if "))
            {
                var ifStmt = ParseIfStatement(lines, ref index, indent);
                if (ifStmt != null)
                {
                    ifStmt.lineNumber = lineNum;
                    statements.Add(ifStmt);
                }
            }
            else if (trimmed.StartsWith("while "))
            {
                var whileStmt = ParseWhileLoop(lines, ref index, indent);
                if (whileStmt != null)
                {
                    whileStmt.lineNumber = lineNum;
                    statements.Add(whileStmt);
                }
            }
            else if (trimmed.StartsWith("for "))
            {
                var forStmt = ParseForLoop(lines, ref index, indent);
                if (forStmt != null)
                {
                    forStmt.lineNumber = lineNum;
                    statements.Add(forStmt);
                }
            }
            else if (trimmed.Contains("=") && !trimmed.Contains("==") && !trimmed.Contains("!=") && 
                     !trimmed.Contains("<=") && !trimmed.Contains(">="))
            {
                // Assignment
                var match = Regex.Match(trimmed, @"^(\w+)\s*=\s*(.+)$");
                if (match.Success)
                {
                    statements.Add(new Assignment
                    {
                        lineNumber = lineNum,
                        varName = match.Groups[1].Value,
                        expression = match.Groups[2].Value
                    });
                }
                else
                {
                    AddFeedback($"Line {lineNum}: Invalid assignment");
                }
                index++;
            }
            else if (trimmed.Contains("(") && trimmed.Contains(")"))
            {
                var funcCall = ParseFunctionCall(trimmed);
                if (funcCall != null)
                {
                    funcCall.lineNumber = lineNum;
                    statements.Add(funcCall);
                }
                else
                {
                    AddFeedback($"Line {lineNum}: Invalid function call");
                }
                index++;
            }
            else
            {
                AddFeedback($"Line {lineNum}: Invalid syntax: {trimmed}");
                index++;
            }
        }

        return statements;
    }

    private IfStatement ParseIfStatement(string[] lines, ref int index, int baseIndent)
    {
        var branches = new List<(string, List<Statement>)>();
        List<Statement> elseBlock = null;

        string line = lines[index].Trim();
        var match = Regex.Match(line, @"^if\s+(.+):$");
        if (!match.Success)
        {
            AddFeedback($"Line {index + 1}: Invalid if statement");
            index++;
            return null;
        }

        string condition = match.Groups[1].Value;
        index++;
        var ifBlock = ParseBlock(lines, ref index, baseIndent + 4);
        branches.Add((condition, ifBlock));

        while (index < lines.Length)
        {
            line = lines[index];
            int indent = GetIndentation(line);
            string trimmed = line.Trim();

            if (indent != baseIndent) break;

            if (trimmed.StartsWith("elif "))
            {
                match = Regex.Match(trimmed, @"^elif\s+(.+):$");
                if (match.Success)
                {
                    condition = match.Groups[1].Value;
                    index++;
                    var elifBlock = ParseBlock(lines, ref index, baseIndent + 4);
                    branches.Add((condition, elifBlock));
                }
                else
                {
                    break;
                }
            }
            else if (trimmed == "else:" || trimmed.StartsWith("else:"))
            {
                index++;
                elseBlock = ParseBlock(lines, ref index, baseIndent + 4);
                break;
            }
            else
            {
                break;
            }
        }

        return new IfStatement { branches = branches, elseBlock = elseBlock };
    }

    private WhileLoop ParseWhileLoop(string[] lines, ref int index, int baseIndent)
    {
        string line = lines[index].Trim();
        var match = Regex.Match(line, @"^while\s+(.+):$");
        if (!match.Success)
        {
            AddFeedback($"Line {index + 1}: Invalid while statement");
            index++;
            return null;
        }

        string condition = match.Groups[1].Value;
        index++;
        var body = ParseBlock(lines, ref index, baseIndent + 4);

        return new WhileLoop { condition = condition, body = body };
    }

    private ForLoop ParseForLoop(string[] lines, ref int index, int baseIndent)
    {
        string line = lines[index].Trim();
        var match = Regex.Match(line, @"^for\s+(\w+)\s+in\s+(.+):$");
        if (!match.Success)
        {
            AddFeedback($"Line {index + 1}: Invalid for statement");
            index++;
            return null;
        }

        string iterator = match.Groups[1].Value;
        string rangeExpr = match.Groups[2].Value;
        index++;
        var body = ParseBlock(lines, ref index, baseIndent + 4);

        return new ForLoop { iterator = iterator, rangeExpr = rangeExpr, body = body };
    }

    private FunctionCall ParseFunctionCall(string line)
    {
        var match = Regex.Match(line, @"^(\w+)\s*\((.*)\)\s*$");
        if (!match.Success) return null;

        string name = match.Groups[1].Value;
        string argString = match.Groups[2].Value.Trim();
        
        var args = ParseArguments(argString);
        
        return new FunctionCall { name = name, args = args.ToArray() };
    }

    private List<string> ParseArguments(string argString)
    {
        var args = new List<string>();
        if (string.IsNullOrWhiteSpace(argString)) return args;

        int parenDepth = 0;
        int start = 0;
        bool inString = false;
        char stringChar = '\0';

        for (int i = 0; i < argString.Length; i++)
        {
            char c = argString[i];

            if (!inString)
            {
                if (c == '"' || c == '\'')
                {
                    inString = true;
                    stringChar = c;
                }
                else if (c == '(') parenDepth++;
                else if (c == ')') parenDepth--;
                else if (c == ',' && parenDepth == 0)
                {
                    args.Add(argString.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            else if (c == stringChar && (i == 0 || argString[i - 1] != '\\'))
            {
                inString = false;
            }
        }

        // Add last argument
        if (start < argString.Length)
        {
            args.Add(argString.Substring(start).Trim());
        }

        return args;
    }

    private List<int> ParseRange(string rangeExpr)
    {
        // Handle range(stop), range(start, stop), range(start, stop, step)
        if (!rangeExpr.StartsWith("range(") || !rangeExpr.EndsWith(")"))
        {
            AddFeedback($"Invalid range expression: {rangeExpr}");
            return null;
        }

        string inner = rangeExpr.Substring(6, rangeExpr.Length - 7);
        var args = ParseArguments(inner);
        
        if (args.Count < 1 || args.Count > 3)
        {
            AddFeedback($"range() takes 1 to 3 arguments, got {args.Count}");
            return null;
        }

        // Evaluate each argument
        int start = 0, stop = 0, step = 1;
        
        try
        {
            if (args.Count == 1)
            {
                stop = ConvertToInt(EvaluateExpression(args[0]));
            }
            else if (args.Count == 2)
            {
                start = ConvertToInt(EvaluateExpression(args[0]));
                stop = ConvertToInt(EvaluateExpression(args[1]));
            }
            else if (args.Count == 3)
            {
                start = ConvertToInt(EvaluateExpression(args[0]));
                stop = ConvertToInt(EvaluateExpression(args[1]));
                step = ConvertToInt(EvaluateExpression(args[2]));
            }
        }
        catch
        {
            AddFeedback($"Error evaluating range arguments");
            return null;
        }

        if (step == 0)
        {
            AddFeedback("range() step cannot be zero");
            return null;
        }

        var result = new List<int>();
        if (step > 0)
        {
            for (int i = start; i < stop; i += step)
                result.Add(i);
        }
        else
        {
            for (int i = start; i > stop; i += step)
                result.Add(i);
        }

        return result;
    }

    private object EvaluateExpression(string expr)
    {
        if (expr == null) return null;
        expr = expr.Trim();

        // Boolean literals
        if (expr == "True" || expr == "true") return true;
        if (expr == "False" || expr == "false") return false;
        if (expr == "None" || expr == "null") return null;

        // String literals
        if ((expr.StartsWith("\"") && expr.EndsWith("\"")) || 
            (expr.StartsWith("'") && expr.EndsWith("'")))
        {
            return expr.Substring(1, expr.Length - 2);
        }

        // Numbers
        if (int.TryParse(expr, out int intVal)) return intVal;
        if (float.TryParse(expr, out float floatVal)) return floatVal;

        // Variables
        if (variables.ContainsKey(expr))
        {
            return variables[expr];
        }

        // List literals [1, 2, 3]
        if (expr.StartsWith("[") && expr.EndsWith("]"))
        {
            string inner = expr.Substring(1, expr.Length - 2);
            var elements = ParseArguments(inner);
            var list = new List<object>();
            foreach (var elem in elements)
            {
                list.Add(EvaluateExpression(elem));
            }
            return list;
        }

        // Binary operators (in order of precedence)
        // Handle comparison operators
        string[] compOps = { "==", "!=", "<=", ">=", "<", ">", " in ", " not in " };
        foreach (var op in compOps)
        {
            if (TryEvaluateBinaryOp(expr, op, out object result))
                return result;
        }

        // Boolean operators
        if (TryEvaluateBinaryOp(expr, " and ", out object andResult))
            return andResult;
        if (TryEvaluateBinaryOp(expr, " or ", out object orResult))
            return orResult;

        // Arithmetic operators
        if (TryEvaluateBinaryOp(expr, "+", out object addResult))
            return addResult;
        if (TryEvaluateBinaryOp(expr, "-", out object subResult))
            return subResult;
        if (TryEvaluateBinaryOp(expr, "*", out object mulResult))
            return mulResult;
        if (TryEvaluateBinaryOp(expr, "/", out object divResult))
            return divResult;
        if (TryEvaluateBinaryOp(expr, "//", out object floorDivResult))
            return floorDivResult;
        if (TryEvaluateBinaryOp(expr, "%", out object modResult))
            return modResult;
        if (TryEvaluateBinaryOp(expr, "**", out object powResult))
            return powResult;

        // Unary not
        if (expr.StartsWith("not "))
        {
            string inner = expr.Substring(4);
            return !EvaluateBool(inner);
        }

        // Parentheses
        if (expr.StartsWith("(") && expr.EndsWith(")"))
        {
            return EvaluateExpression(expr.Substring(1, expr.Length - 2));
        }

        // If nothing matches, return as string
        return expr;
    }

    private bool TryEvaluateBinaryOp(string expr, string op, out object result)
    {
        result = null;
        
        // Find operator not inside strings or parentheses
        int opIndex = FindOperatorIndex(expr, op);
        if (opIndex == -1) return false;

        string left = expr.Substring(0, opIndex).Trim();
        string right = expr.Substring(opIndex + op.Length).Trim();

        object leftVal = EvaluateExpression(left);
        object rightVal = EvaluateExpression(right);

        switch (op)
        {
            case "==":
                result = Equals(leftVal, rightVal);
                return true;
            case "!=":
                result = !Equals(leftVal, rightVal);
                return true;
            case "<":
                result = ConvertToFloat(leftVal) < ConvertToFloat(rightVal);
                return true;
            case ">":
                result = ConvertToFloat(leftVal) > ConvertToFloat(rightVal);
                return true;
            case "<=":
                result = ConvertToFloat(leftVal) <= ConvertToFloat(rightVal);
                return true;
            case ">=":
                result = ConvertToFloat(leftVal) >= ConvertToFloat(rightVal);
                return true;
            case " in ":
                if (rightVal is List<object> list)
                {
                    result = list.Contains(leftVal);
                    return true;
                }
                if (rightVal is string str && leftVal is string substr)
                {
                    result = str.Contains(substr);
                    return true;
                }
                result = false;
                return true;
            case " not in ":
                if (rightVal is List<object> list2)
                {
                    result = !list2.Contains(leftVal);
                    return true;
                }
                if (rightVal is string str2 && leftVal is string substr2)
                {
                    result = !str2.Contains(substr2);
                    return true;
                }
                result = true;
                return true;
            case " and ":
                result = EvaluateBool(left) && EvaluateBool(right);
                return true;
            case " or ":
                result = EvaluateBool(left) || EvaluateBool(right);
                return true;
            case "+":
                if (leftVal is string || rightVal is string)
                {
                    result = leftVal?.ToString() + rightVal?.ToString();
                }
                else
                {
                    result = ConvertToFloat(leftVal) + ConvertToFloat(rightVal);
                }
                return true;
            case "-":
                result = ConvertToFloat(leftVal) - ConvertToFloat(rightVal);
                return true;
            case "*":
                if (leftVal is string s && rightVal is int n)
                {
                    result = string.Concat(System.Linq.Enumerable.Repeat(s, n));
                }
                else
                {
                    result = ConvertToFloat(leftVal) * ConvertToFloat(rightVal);
                }
                return true;
            case "/":
                float divisor = ConvertToFloat(rightVal);
                if (divisor == 0)
                {
                    AddFeedback("Division by zero");
                    result = 0f;
                }
                else
                {
                    result = ConvertToFloat(leftVal) / divisor;
                }
                return true;
            case "//":
                float divisor2 = ConvertToFloat(rightVal);
                if (divisor2 == 0)
                {
                    AddFeedback("Division by zero");
                    result = 0;
                }
                else
                {
                    result = (int)(ConvertToFloat(leftVal) / divisor2);
                }
                return true;
            case "%":
                result = ConvertToFloat(leftVal) % ConvertToFloat(rightVal);
                return true;
            case "**":
                result = Mathf.Pow(ConvertToFloat(leftVal), ConvertToFloat(rightVal));
                return true;
        }

        return false;
    }

    private int FindOperatorIndex(string expr, string op)
    {
        int parenDepth = 0;
        bool inString = false;
        char stringChar = '\0';

        for (int i = 0; i < expr.Length - op.Length + 1; i++)
        {
            if (!inString)
            {
                if (expr[i] == '"' || expr[i] == '\'')
                {
                    inString = true;
                    stringChar = expr[i];
                }
                else if (expr[i] == '(') parenDepth++;
                else if (expr[i] == ')') parenDepth--;
                else if (parenDepth == 0 && expr.Substring(i, Mathf.Min(op.Length, expr.Length - i)) == op)
                {
                    return i;
                }
            }
            else if (expr[i] == stringChar && (i == 0 || expr[i - 1] != '\\'))
            {
                inString = false;
            }
        }

        return -1;
    }

    private bool EvaluateBool(string expr)
    {
        object result = EvaluateExpression(expr);
        
        if (result is bool b) return b;
        if (result is int i) return i != 0;
        if (result is float f) return f != 0;
        if (result is string s) return !string.IsNullOrEmpty(s);
        if (result is List<object> list) return list.Count > 0;
        if (result == null) return false;
        
        return true;
    }

    private int ConvertToInt(object val)
    {
        if (val is int i) return i;
        if (val is float f) return (int)f;
        if (val is bool b) return b ? 1 : 0;
        if (val is string s && int.TryParse(s, out int result)) return result;
        return 0;
    }

    private float ConvertToFloat(object val)
    {
        if (val is float f) return f;
        if (val is int i) return i;
        if (val is bool b) return b ? 1f : 0f;
        if (val is string s && float.TryParse(s, out float result)) return result;
        return 0f;
    }

    private int GetIndentation(string line)
    {
        int spaces = 0;
        foreach (char c in line)
        {
            if (c == ' ') spaces++;
            else if (c == '\t') spaces += 4;
            else break;
        }
        return spaces;
    }

    private IEnumerator ExecuteStatements(List<Statement> statements)
    {
        isRunning = true;

        foreach (var stmt in statements)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                isRunning = false;
                RunLimitManager.Instance?.RunCompleted();
                yield break;
            }

            currentLine = stmt.lineNumber;
            yield return StartCoroutine(stmt.Execute(this));
            yield return new WaitForSeconds(commandDelay);
        }

        isRunning = false;
        RunLimitManager.Instance?.RunCompleted();

        if (feedbackText != null)
        {
            feedbackText.text = feedback.Count > 0 ? string.Join("\n", feedback) : "Code executed successfully!";
        }
    }

    public void AddFeedback(string message)
    {
        feedback.Add(message);
    }

    private void OnCodeChanged(string text)
    {
        if (highlightedText == null) return;
        highlightedText.text = Highlight(text);
    }

    private string Highlight(string code)
    {
        string result = code;
        
        string keywordColor = "#c586c0";
        string numberColor = "#b5cea8";
        string stringColor = "#ce9178";
        string commentColor = "#6a9955";
        string functionColor = "#569cd6";
        string builtinColor = "#4ec9b0";

        // Comments (must be first to avoid highlighting inside comments)
        result = Regex.Replace(result, @"#.*$", m => $"<color={commentColor}>{m.Value}</color>", RegexOptions.Multiline);
        
        // Strings (before other replacements to avoid highlighting inside strings)
        result = Regex.Replace(result, @"""([^""]*)""", m => $"<color={stringColor}>{m.Value}</color>");
        result = Regex.Replace(result, @"'([^']*)'", m => $"<color={stringColor}>{m.Value}</color>");
        
        // Keywords
        result = Regex.Replace(result, @"\b(if|elif|else|for|in|while|and|or|not|True|False|None|range|break|continue|pass|return)\b", 
            m => $"<color={keywordColor}>{m.Value}</color>");
        
        // Numbers
        result = Regex.Replace(result, @"\b\d+\.?\d*\b", m => $"<color={numberColor}>{m.Value}</color>");
        
        // Built-in functions (Remember To add a new build in fucntion here)
        // result = Regex.Replace(result, @"\b(print|len|str|int|float|bool|list|dict)\b", 
        result = Regex.Replace(result, @"\b(nothing)\b",
            m => $"<color={builtinColor}>{m.Value}</color>");
        
        // Registered commands
        foreach (var cmd in commandRegistry.Keys)
        {
            result = Regex.Replace(result, $@"\b{Regex.Escape(cmd)}\b(?=\s*\()", 
                m => $"<color={functionColor}>{m.Value}</color>");
        }
        
        // Direction enums (common in games)
        result = Regex.Replace(result, @"\b(Up|Down|Left|Right)\b", 
            m => $"<color={numberColor}>{m.Value}</color>");

        return result;
    }
    
    // Getter 
    public bool IsRunning
    {
        get { return isRunning; }
    }

    public void AbortExecutionOnWin()
    {
        StopAllCoroutines();
        isRunning = false;
        variables.Clear();
        feedback.Clear();
        if (feedbackText != null)
        {
            feedbackText.text = "Game finished. Commands are disabled.";
        }
    }

}
