using Dunet;

namespace TestBed.Elmish;



[Union]
public partial record Msg
{
    partial record Increment;
    partial record Decrement;
    partial record SetStepSize(int Size);
}

public readonly record struct Model(int Count, int StepSize)
{
    public static Model Init() => new(0, 1);
}

public static class AppExample
{
    public static Model Update(Msg msg, Model model)
    {
        return msg.Match(
            _ => model with { Count = model.Count + model.StepSize },
            _ => model with { Count = model.Count - model.StepSize },
            step => model with { StepSize = step.Size }
            );
    }
}