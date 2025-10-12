namespace Simulator.Domain
{
    // This is the return type of the PostPropagation callback function and lets the viewmodel inform the engine
    // that the animation should stop at a later state index with a given outcome.
    public class PostPropagationResponse
    {
        public int? IndexOfLastState { get; set; }
        public string Outcome { get; set; }
    }
}