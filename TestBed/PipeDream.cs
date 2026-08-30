using Utils;

namespace TestBed;

public static class PipeDream
{
    extension(string) {
        public static Parth operator /(string a, string b) => Parth.Create(a, b);
    }

    extension<A, B>(A)
    {
        public static B operator |(A a, Func<A, B> f) => f(a);
    }

    extension<A, B, C>(B)
    {
        public static Func<A, C> operator |(B b, Func<A, B, C> f) => x => f(x, b);
    }
    
    extension(Parth)
    {
        public static Parth operator /(Parth p, string s) => p.Append(s);
    }

}

public class Parth(string path)
{
    public Parth Append(string s) => new(path + "/" + s);

    public static Parth Create(string a, string b) => new(a + "/" + b);
}

public static class Tex
{
    public static void Main()
    {
        var basePath = new Parth("");
        var route = "";
        var action = "";

        var x = basePath/"route"/action;
    }
}