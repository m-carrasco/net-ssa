using Mono.Cecil.Cil;

namespace MyBenchmarks
{
    public class BodyWrapper
    {
        public BodyWrapper(Func<MethodBody, String> Print) { this.Print = Print; }
        Func<MethodBody, String> Print;
        public MethodBody Body;

        public override string ToString() => Print(Body);
    }
};
