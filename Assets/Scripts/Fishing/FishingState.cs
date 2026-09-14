public enum FishingState //Determines and tracks which state of fishing the player is in
{
    NotFishing,      // Standing normally
    Sitting,         // Sitting in the chair, not casting
    Casting,         // Cast animation / action
    WaitingForFish,  // Line is in water
    FishHooked,      // Fish is hooked
    Withdrawing      // Pulling line back out
}