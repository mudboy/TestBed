namespace TestBed;

interface IPartA
{
    string DoIt(int x);
}

interface IPartB
{
    void DoneIt(decimal p);
}

public partial class Parts : IPartA
{
    private string _xx = "alk";
    
    private void Foo() {}
    
    public string DoIt(int x)
    {
        throw new NotImplementedException();
    }
}

public partial class Parts : IPartB
{
    public void DoneIt(decimal p)
    {
        _xx = "";
        Foo();
    }
}