using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FryMath.Core.CAS;

public enum TokenType
{
    Number,
    Identifier,
    Plus,
    Minus,
    Multiply,
    Divide,
    Power,
    OpenParen,
    CloseParen,
    Comma,
    Equals,
    EndOfFile
}

public sealed record Token(TokenType Type, string Text, double NumberValue, int Position)
{
    public override string ToString() => $"{Type}: '{Text}' at {Position}";
}

public static class MathTokenizer
{
    private static readonly HashSet<string> KnownFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "sin", "cos", "tan", "asin", "acos", "atan", "arcsin", "arccos", "arctan",
        "sinh", "cosh", "tanh", "exp", "ln", "log", "log10", "log2", "sqrt", "cbrt",
        "abs", "floor", "ceil", "round", "sign", "sgn", "atan2", "min", "max"
    };

    public static List<Token> Tokenize(string input)
    {
        var rawTokens = new List<Token>();
        if (string.IsNullOrWhiteSpace(input))
        {
            rawTokens.Add(new Token(TokenType.EndOfFile, "", 0, 0));
            return rawTokens;
        }

        int i = 0;
        int len = input.Length;

        while (i < len)
        {
            char c = input[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            int startPos = i;

            // Numbers (including decimal point if followed by digit)
            if (char.IsDigit(c) || (c == '.' && i + 1 < len && char.IsDigit(input[i + 1])))
            {
                var sb = new StringBuilder();
                bool hasDot = false;
                while (i < len && (char.IsDigit(input[i]) || (!hasDot && input[i] == '.')))
                {
                    if (input[i] == '.') hasDot = true;
                    sb.Append(input[i]);
                    i++;
                }

                // Optional scientific notation (e.g. 1e-5, 2.5E+3)
                if (i < len && (input[i] == 'e' || input[i] == 'E') && i + 1 < len &&
                    (char.IsDigit(input[i + 1]) || ((input[i + 1] == '+' || input[i + 1] == '-') && i + 2 < len && char.IsDigit(input[i + 2]))))
                {
                    sb.Append(input[i++]); // 'e' or 'E'
                    if (input[i] == '+' || input[i] == '-') sb.Append(input[i++]);
                    while (i < len && char.IsDigit(input[i])) sb.Append(input[i++]);
                }

                string numStr = sb.ToString();
                double val = double.Parse(numStr, CultureInfo.InvariantCulture);
                rawTokens.Add(new Token(TokenType.Number, numStr, val, startPos));
                continue;
            }

            // Identifiers (variable names, functions, greek letters like π, θ, φ)
            if (char.IsLetter(c) || c == '_' || c == 'π' || c == 'θ' || c == 'φ' || c == 'λ' || c == 'ω')
            {
                var sb = new StringBuilder();
                while (i < len && (char.IsLetterOrDigit(input[i]) || input[i] == '_' || input[i] == 'π' || input[i] == 'θ' || input[i] == 'φ' || input[i] == 'λ' || input[i] == 'ω'))
                {
                    sb.Append(input[i]);
                    i++;
                }
                string ident = sb.ToString();
                rawTokens.Add(new Token(TokenType.Identifier, ident, 0, startPos));
                continue;
            }

            // Operators and punctuation
            switch (c)
            {
                case '+':
                    rawTokens.Add(new Token(TokenType.Plus, "+", 0, startPos));
                    i++;
                    break;
                case '-':
                case '−': // unicode minus
                    rawTokens.Add(new Token(TokenType.Minus, "-", 0, startPos));
                    i++;
                    break;
                case '*':
                case '×':
                case '·':
                    if (i + 1 < len && input[i + 1] == '*')
                    {
                        rawTokens.Add(new Token(TokenType.Power, "**", 0, startPos));
                        i += 2;
                    }
                    else
                    {
                        rawTokens.Add(new Token(TokenType.Multiply, "*", 0, startPos));
                        i++;
                    }
                    break;
                case '/':
                case '÷':
                    rawTokens.Add(new Token(TokenType.Divide, "/", 0, startPos));
                    i++;
                    break;
                case '^':
                    rawTokens.Add(new Token(TokenType.Power, "^", 0, startPos));
                    i++;
                    break;
                case '(':
                    rawTokens.Add(new Token(TokenType.OpenParen, "(", 0, startPos));
                    i++;
                    break;
                case ')':
                    rawTokens.Add(new Token(TokenType.CloseParen, ")", 0, startPos));
                    i++;
                    break;
                case ',':
                    rawTokens.Add(new Token(TokenType.Comma, ",", 0, startPos));
                    i++;
                    break;
                case '=':
                    rawTokens.Add(new Token(TokenType.Equals, "=", 0, startPos));
                    i++;
                    break;
                case '²': // superscript 2
                    rawTokens.Add(new Token(TokenType.Power, "^", 0, startPos));
                    rawTokens.Add(new Token(TokenType.Number, "2", 2.0, startPos));
                    i++;
                    break;
                case '³': // superscript 3
                    rawTokens.Add(new Token(TokenType.Power, "^", 0, startPos));
                    rawTokens.Add(new Token(TokenType.Number, "3", 3.0, startPos));
                    i++;
                    break;
                default:
                    throw new FormatException($"Unexpected character '{c}' at position {startPos}");
            }
        }

        // Insert implicit multiplication tokens where standard mathematical convention applies
        var tokensWithImplicit = new List<Token>();
        for (int k = 0; k < rawTokens.Count; k++)
        {
            var curr = rawTokens[k];
            tokensWithImplicit.Add(curr);

            if (k + 1 < rawTokens.Count)
            {
                var next = rawTokens[k + 1];

                bool needsMultiply = false;

                // 2x -> 2 * x
                if (curr.Type == TokenType.Number && next.Type == TokenType.Identifier)
                    needsMultiply = true;
                // 2(x+1) -> 2 * (x+1)
                else if (curr.Type == TokenType.Number && next.Type == TokenType.OpenParen)
                    needsMultiply = true;
                // (x+1)(x-1) -> (x+1) * (x-1)
                else if (curr.Type == TokenType.CloseParen && next.Type == TokenType.OpenParen)
                    needsMultiply = true;
                // (x+1)x -> (x+1) * x
                else if (curr.Type == TokenType.CloseParen && next.Type == TokenType.Identifier)
                    needsMultiply = true;
                // x(x+1) -> x * (x+1), unless x is a function name like sin(x)
                else if (curr.Type == TokenType.Identifier && next.Type == TokenType.OpenParen && !KnownFunctions.Contains(curr.Text))
                    needsMultiply = true;
                // x y -> x * y (variable times variable or constant)
                else if (curr.Type == TokenType.Identifier && next.Type == TokenType.Identifier && !KnownFunctions.Contains(curr.Text))
                    needsMultiply = true;

                if (needsMultiply)
                {
                    tokensWithImplicit.Add(new Token(TokenType.Multiply, "*", 0, curr.Position));
                }
            }
        }

        tokensWithImplicit.Add(new Token(TokenType.EndOfFile, "", 0, len));
        return tokensWithImplicit;
    }
}
