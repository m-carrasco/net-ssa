// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Factorial(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: IL_0000: s0 = a0
// CHECK: IL_0001: s1 = 0
// CHECK: IL_0002: br IL_0009 if s0 >= s1
// CHECK: IL_0007: s0 = 1
// CHECK: IL_0008: ret s0
// CHECK: IL_0009: s0 = 1
// CHECK: IL_000a: l0 = s0
// CHECK: IL_000b: s0 = 1
// CHECK: IL_000c: l1 = s0
// CHECK: IL_000d: br IL_001a
// CHECK: IL_0012: s0 = l0
// CHECK: IL_0013: s1 = l1
// CHECK: IL_0014: s0 = s0 * s1
// CHECK: IL_0015: l0 = s0
// CHECK: IL_0016: s0 = l1
// CHECK: IL_0017: s1 = 1
// CHECK: IL_0018: s0 = s0 + s1
// CHECK: IL_0019: l1 = s0
// CHECK: IL_001a: s0 = l1
// CHECK: IL_001b: s1 = a0
// CHECK: IL_001c: br IL_0012 if s0 <= s1
// CHECK: IL_0021: s0 = l0
// CHECK: IL_0022: ret s0

public class Test
{
    public static int Factorial(int a)
    {
        if (a < 0)
            return 1;

        int r = 1;
        for (int i = 1; i <= a; i++)
        {
            r *= i;
        }

        return r;
    }
}
