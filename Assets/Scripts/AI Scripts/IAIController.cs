// IAIController.cs
public interface IAIController
{
    int PlayerId { get; }
    void ExecuteTurn();
    void Initialize(GameManager gameManager);
    string GetAITypeName();
}