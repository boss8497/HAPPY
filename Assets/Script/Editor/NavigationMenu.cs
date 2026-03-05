using System;
using Expression;
using UnityEditor;
using UnityEngine;

public static class NavigationMenu
{
    [MenuItem("Tools/수식 테스트")]
    public static void TestExpression() {
        var expr = new Expression.Expression("(level * 0.5) + 10 + grade * (8 + level)");

        using var provider = new ValueContext(
            new ValueProvider()
                .Add("level", 2)
                .Add("grade", 3)
                .Add("tier", 2)
        );
        var v = expr.Calc();
        Console.WriteLine($"{v}");
    }
    
}
