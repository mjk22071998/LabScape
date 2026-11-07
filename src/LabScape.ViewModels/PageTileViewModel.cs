namespace LabScape.ViewModels;

public class PageTileViewModel
{
    public int PageIndex { get; set; }
    public int Column { get; set; }
    public int Row { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}
