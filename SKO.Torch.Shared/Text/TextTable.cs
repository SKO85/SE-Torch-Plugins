using System.Collections.Generic;

namespace SKO.Torch.Shared.Text
{
    public class TextTable
    {
        public Dictionary<string, TextTableColumn> Columns { get; set; }
        public Dictionary<string, TextTableRow> Rows { get; set; }
    }
}