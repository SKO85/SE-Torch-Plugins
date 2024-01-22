using SKO.Bounty.Utils;
using System.Collections.Generic;
using System.Text;

namespace SKO.Bounty.Models
{
    public class TextTable
    {
        public List<TextTableColumn> Columns;
        public List<TextTableRow> Rows;

        public TextTable(string name)
        {
            Name = name;
            Columns = new List<TextTableColumn>();
            Rows = new List<TextTableRow>();
        }

        public string Name { get; set; }

        public StringBuilder GetOutput()
        {
            var sb = new StringBuilder();
            foreach (var col in Columns)
                if (col.MaxWidth == 0)
                {
                    var colIndex = Columns.IndexOf(col);
                    col.MaxWidth = col.Name.Length + 5;
                    foreach (var row in Rows)
                    {
                        var cellWidth = row.Cells[colIndex].Length + 5;
                        if (cellWidth > col.MaxWidth)
                            col.MaxWidth = cellWidth;
                    }
                }

            sb.AppendLine($"> {Name}:");
            float addIn = 0;
            foreach (var col in Columns)
            {
                var colIndex = Columns.IndexOf(col);
                addIn = TableFormatting.AlignWord(sb, col.Name, col.MaxWidth + addIn);
            }

            sb.Append('\n');
            sb.AppendLine("___________________________________________________________________");

            foreach (var row in Rows)
            {
                addIn = 0;
                foreach (var col in Columns)
                {
                    var colIndex = Columns.IndexOf(col);
                    var cellValue = string.IsNullOrEmpty(col.Format)
                        ? row.Cells[colIndex]
                        : string.Format(col.Format, row.Cells[colIndex]);
                    addIn = TableFormatting.AlignWord(sb, cellValue, col.MaxWidth + addIn);
                }

                sb.Append('\n');
            }

            return sb;
        }
    }

    public class TextTableColumn
    {
        public TextTableColumn(string name, string format = "", int maxWidth = 0)
        {
            Name = name;
            MaxWidth = maxWidth;
            Format = format;
        }

        public string Name { get; set; }
        public int MaxWidth { get; set; }
        public string Format { get; set; }
    }

    public class TextTableRow
    {
        public List<string> Cells { get; set; } = new List<string>();
    }
}