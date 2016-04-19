﻿namespace Gu.State
{
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>A value difference X != Y </summary>
    public class ValueDiff : Diff
    {
        /// <summary>Initializes a new instance of the <see cref="ValueDiff"/> class.</summary>
        /// <param name="xValue">The x value.</param>
        /// <param name="yValue">The y value.</param>
        public ValueDiff(object xValue, object yValue)
        {
            this.X = xValue;
            this.Y = yValue;
        }

        public ValueDiff(object xValue, object yValue, IReadOnlyList<SubDiff> diffs)
            : base(diffs)
        {
            this.X = xValue;
            this.Y = yValue;
        }

        /// <summary>Gets the X value.</summary>
        public object X { get; }

        /// <summary>Gets the Y value.</summary>
        public object Y { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"x: {this.X ?? "null"} y: {this.Y ?? "null"} diffs: {this.Diffs.Count}";
        }

        /// <inheritdoc />
        public override string ToString(string tabString, string newLine)
        {
            if (this.Diffs.Count == 0)
            {
                return $"{this.X.GetType().PrettyName()} x: {this.X ?? "null"} y: {this.Y ?? "null"}";
            }

            using (var writer = new IndentedTextWriter(new StringWriter(), tabString) { NewLine = newLine })
            {
                writer.Write(this.X.GetType().PrettyName());
                using (var disposer = BorrowReferenceList())
                {
                    this.WriteDiffs(writer, disposer.Value);
                }

                return writer.InnerWriter.ToString();
            }
        }

        internal override IndentedTextWriter WriteDiffs(IndentedTextWriter writer, List<SubDiff> written)
        {
            writer.Indent++;
            foreach (var diff in this.Diffs)
            {
                writer.WriteLine();
                diff.WriteDiffs(writer, written);
            }

            writer.Indent--;
            return writer;
        }
    }
}