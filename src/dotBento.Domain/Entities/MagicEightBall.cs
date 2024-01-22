namespace dotBento.Domain.Entities;

public class MagicEightBall
{
    private static readonly List<string> Responses = new List<string>
    {
        "Maybe.",
        "Certainly not.",
        "I hope so.",
        "Not in your wildest dreams.",
        "There is a good chance.",
        "Quite likely.",
        "I think so.",
        "I hope not.",
        "I hope so.",
        "Never!",
        "Fuhgeddaboudit.",
        "Ahaha! Really?!?",
        "Pfft.",
        "Sorry, bucko.",
        "Hell, yes.",
        "Hell to the no.",
        "The future is bleak.",
        "The future is uncertain.",
        "I would rather not say.",
        "Who cares?",
        "Possibly.",
        "Never, ever, ever.",
        "There is a small chance.",
        "Yes!"
    };

    public static string RandomResponse()
    {
        var random = new Random();
        var index = random.Next(Responses.Count);
        return Responses[index];
    }
}