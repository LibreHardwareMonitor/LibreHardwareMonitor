// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReportStyle.cs" company="OxyPlot">
//   The MIT License (MIT)
//
//   Copyright (c) 2012 Oystein Bjorke
//
//   Permission is hereby granted, free of charge, to any person obtaining a
//   copy of this software and associated documentation files (the
//   "Software"), to deal in the Software without restriction, including
//   without limitation the rights to use, copy, modify, merge, publish,
//   distribute, sublicense, and/or sell copies of the Software, and to
//   permit persons to whom the Software is furnished to do so, subject to
//   the following conditions:
//
//   The above copyright notice and this permission notice shall be included
//   in all copies or substantial portions of the Software.
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//   IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//   CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//   TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//   SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary>
//   The report style.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Reporting
{
    /// <summary>
    /// The report style.
    /// </summary>
    public class ReportStyle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportStyle"/> class.
        /// </summary>
        /// <param name="titleFontFamily">
        /// The title font family.
        /// </param>
        /// <param name="bodyTextFontFamily">
        /// The body text font family.
        /// </param>
        /// <param name="tableTextFontFamily">
        /// The table text font family.
        /// </param>
        public ReportStyle(
            string titleFontFamily = "Arial",
            string bodyTextFontFamily = "Verdana",
            string tableTextFontFamily = "Courier New")
        {
            this.DefaultStyle = new ParagraphStyle { FontFamily = bodyTextFontFamily, FontSize = 11, SpacingAfter = 10 };

            this.HeaderStyles = new ParagraphStyle[5];
            this.HeaderStyles[0] = new ParagraphStyle
                {
                   BasedOn = this.DefaultStyle, FontFamily = titleFontFamily, SpacingBefore = 12, SpacingAfter = 3
                };
            for (int i = 1; i < this.HeaderStyles.Length; i++)
            {
                this.HeaderStyles[i] = new ParagraphStyle { BasedOn = this.HeaderStyles[i - 1] };
            }

            for (int i = 0; i < this.HeaderStyles.Length; i++)
            {
                this.HeaderStyles[i].Bold = true;
            }

            this.HeaderStyles[0].FontSize = 16;
            this.HeaderStyles[1].FontSize = 14;
            this.HeaderStyles[2].FontSize = 13;
            this.HeaderStyles[3].FontSize = 12;
            this.HeaderStyles[4].FontSize = 11;

            this.HeaderStyles[0].PageBreakBefore = true;
            this.HeaderStyles[1].PageBreakBefore = false;

            this.BodyTextStyle = new ParagraphStyle { BasedOn = this.DefaultStyle };
            this.FigureTextStyle = new ParagraphStyle { BasedOn = this.DefaultStyle, Italic = true };

            this.TableTextStyle = new ParagraphStyle
                {
                    BasedOn = this.DefaultStyle,
                    FontFamily = tableTextFontFamily,
                    SpacingAfter = 0,
                    LeftIndentation = 3,
                    RightIndentation = 3
                };
            this.TableHeaderStyle = new ParagraphStyle { BasedOn = this.TableTextStyle, Bold = true };
            this.TableCaptionStyle = new ParagraphStyle
                {
                   BasedOn = this.DefaultStyle, Italic = true, SpacingBefore = 10, SpacingAfter = 3
                };

            this.Margins = new OxyThickness(25);

            this.FigureTextFormatString = "Figure {0}. {1}";
            this.TableCaptionFormatString = "Table {0}. {1}";
        }

        /// <summary>
        /// Gets or sets BodyTextStyle.
        /// </summary>
        public ParagraphStyle BodyTextStyle { get; set; }

        /// <summary>
        /// Gets or sets DefaultStyle.
        /// </summary>
        public ParagraphStyle DefaultStyle { get; set; }

        /// <summary>
        /// Gets or sets FigureTextFormatString.
        /// </summary>
        public string FigureTextFormatString { get; set; }

        /// <summary>
        /// Gets or sets FigureTextStyle.
        /// </summary>
        public ParagraphStyle FigureTextStyle { get; set; }

        /// <summary>
        /// Gets or sets HeaderStyles.
        /// </summary>
        public ParagraphStyle[] HeaderStyles { get; set; }

        /// <summary>
        /// Gets or sets the page margins (mm).
        /// </summary>
        public OxyThickness Margins { get; set; }

        // todo: should the FormatStrings be in the Report class?

        /// <summary>
        /// Gets or sets TableCaptionFormatString.
        /// </summary>
        public string TableCaptionFormatString { get; set; }

        /// <summary>
        /// Gets or sets TableCaptionStyle.
        /// </summary>
        public ParagraphStyle TableCaptionStyle { get; set; }

        /// <summary>
        /// Gets or sets TableHeaderStyle.
        /// </summary>
        public ParagraphStyle TableHeaderStyle { get; set; }

        /// <summary>
        /// Gets or sets TableTextStyle.
        /// </summary>
        public ParagraphStyle TableTextStyle { get; set; }

    }
}