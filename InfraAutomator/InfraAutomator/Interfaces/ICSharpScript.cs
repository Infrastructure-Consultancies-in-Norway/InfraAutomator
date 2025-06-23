namespace InfraAutomator.Interfaces
{
    public interface ICSharpScript
    {
        string Name { get; set; }
        string Description { get; set; }
        string ScriptPath { get; set; }
        Dictionary<string, string> Parameters { get; set; }

        void Execute();

    }
}