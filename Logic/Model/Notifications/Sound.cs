namespace rpg;
public class SoundNotification(int noiseRange, int x, int y)
{
    public readonly int NoiseLevel = noiseRange;
    public (int x, int y) source = (x, y);
}
