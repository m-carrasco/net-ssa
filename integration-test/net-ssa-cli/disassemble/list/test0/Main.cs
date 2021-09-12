// RUN: %mcs -target:exe -out:%T/Main.exe %s

using System;
using System.Reflection;
using System.Collections;

public class Test
{
    static public void Main(String[] args)
    {
        Type t = typeof(ArrayList);
        Assembly assem = Assembly.GetAssembly(t);
        Console.WriteLine(assem.Location);
    }
}