/// <summary>
/// Result of validating a prepared order against a recipe.
/// Used for player feedback — each value represents a different failure reason.
/// </summary>
public enum OrderResult
{
    Perfect,
    WrongIngredients,
    Raw,
    Burnt
}
