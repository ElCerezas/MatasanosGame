public interface ISnappable
{
    bool isSnapped { get; }
    SnapZone currentZone { get; }
    public abstract void SnapTo(SnapZone zone);
    public abstract void Unsnap();
}
