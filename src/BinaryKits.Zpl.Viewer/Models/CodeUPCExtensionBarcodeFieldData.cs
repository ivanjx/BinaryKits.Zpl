using BinaryKits.Zpl.Label;

namespace BinaryKits.Zpl.Viewer.Models;

public class CodeUPCExtensionBarcodeFieldData : FieldDataBase
{
    public FieldOrientation FieldOrientation { get; set; }
    public int Height { get; set; }
    public bool PrintInterpretationLine { get; set; }
    public bool PrintInterpretationLineAboveCode { get; set; }
}
