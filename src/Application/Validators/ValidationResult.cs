namespace Netemplate.Application.Validators;

public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public Dictionary<string, List<string>> Errors { get; } = new();

    public void Add(string key, string message)
    {
        if (!Errors.TryGetValue(key, out var list))
        {
            list = new List<string>();
            Errors[key] = list;
        }
        list.Add(message);
    }
}
