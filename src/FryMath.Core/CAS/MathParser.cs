using System;
using System.Collections.Generic;

namespace FryMath.Core.CAS;

public sealed class MathParser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _index;

    public MathParser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _index = 0;
    }

    public static MathNode Parse(string expression)
    {
        var tokens = MathTokenizer.Tokenize(expression);
        var parser = new MathParser(tokens);
        var node = parser.ParseExpression();

        if (parser.Current.Type != TokenType.EndOfFile)
        {
            throw new FormatException($"Unexpected token '{parser.Current.Text}' at position {parser.Current.Position}");
        }

        return node;
    }

    private Token Current => _index < _tokens.Count ? _tokens[_index] : _tokens[^1];

    private Token Advance()
    {
        var tok = Current;
        if (_index < _tokens.Count) _index++;
        return tok;
    }

    private bool Match(TokenType type)
    {
        if (Current.Type == type)
        {
            Advance();
            return true;
        }
        return false;
    }

    private Token Expect(TokenType type, string message)
    {
        if (Current.Type == type) return Advance();
        throw new FormatException($"{message} Found '{Current.Text}' at position {Current.Position}");
    }

    // Expression -> Term ( ('+' | '-') Term )*
    public MathNode ParseExpression()
    {
        var node = ParseTerm();

        while (true)
        {
            if (Match(TokenType.Plus))
            {
                var right = ParseTerm();
                node = new BinaryOpNode(MathOp.Add, node, right);
            }
            else if (Match(TokenType.Minus))
            {
                var right = ParseTerm();
                node = new BinaryOpNode(MathOp.Subtract, node, right);
            }
            else
            {
                break;
            }
        }

        return node;
    }

    // Term -> Power ( ('*' | '/') Power )*
    private MathNode ParseTerm()
    {
        var node = ParsePower();

        while (true)
        {
            if (Match(TokenType.Multiply))
            {
                var right = ParsePower();
                node = new BinaryOpNode(MathOp.Multiply, node, right);
            }
            else if (Match(TokenType.Divide))
            {
                var right = ParsePower();
                node = new BinaryOpNode(MathOp.Divide, node, right);
            }
            else
            {
                break;
            }
        }

        return node;
    }

    // Power -> Unary ( '^' Power )?   (Right-associative)
    private MathNode ParsePower()
    {
        var node = ParseUnary();

        if (Match(TokenType.Power))
        {
            var right = ParsePower(); // Right-associative recursion
            node = new BinaryOpNode(MathOp.Power, node, right);
        }

        return node;
    }

    // Unary -> ('+' | '-') Unary | Primary
    private MathNode ParseUnary()
    {
        if (Match(TokenType.Minus))
        {
            var operand = ParseUnary();
            return new UnaryOpNode(MathUnaryOp.Negate, operand);
        }
        if (Match(TokenType.Plus))
        {
            return ParseUnary();
        }

        return ParsePrimary();
    }

    // Primary -> Number | Identifier ('(' Arguments? ')')? | '(' Expression ')'
    private MathNode ParsePrimary()
    {
        var token = Current;

        if (Match(TokenType.Number))
        {
            return new NumberNode(token.NumberValue);
        }

        if (Match(TokenType.Identifier))
        {
            string name = token.Text;

            // Check if it is a function call like sin(x)
            if (Match(TokenType.OpenParen))
            {
                var args = new List<MathNode>();
                if (Current.Type != TokenType.CloseParen)
                {
                    while (true)
                    {
                        args.Add(ParseExpression());
                        if (Match(TokenType.Comma)) continue;
                        break;
                    }
                }
                Expect(TokenType.CloseParen, "Expected ')' closing function arguments.");
                return new FunctionCallNode(name, args);
            }

            return new VariableNode(name);
        }

        if (Match(TokenType.OpenParen))
        {
            var inner = ParseExpression();
            Expect(TokenType.CloseParen, "Expected closing ')' after parenthesized expression.");
            return inner;
        }

        throw new FormatException($"Expected expression but found '{token.Text}' at position {token.Position}");
    }
}
