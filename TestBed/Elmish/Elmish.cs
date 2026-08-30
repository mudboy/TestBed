namespace TestBed.Elmish;

public union Msg(Increment, Decrement, SetStepSize);

public record struct Increment;
public record struct Decrement;
public readonly record struct SetStepSize(int Size);



public readonly record struct Model(int Count, int StepSize)
{
    public static Model Init() => new(0, 1);
}

public static class AppExample
{
    public static Model Update(Msg msg, Model model)
    {
        // return msg.Match(
        //     _ => model with { Count = model.Count + model.StepSize },
        //     _ => model with { Count = model.Count - model.StepSize },
        //     step => model with { StepSize = step.Size }
        //     );

        return msg switch
        {
            Increment => model with { Count = model.Count + model.StepSize },
            Decrement => model with { Count = model.Count - model.StepSize },
            SetStepSize(var size) => model with { StepSize = size }
        };
    }
}