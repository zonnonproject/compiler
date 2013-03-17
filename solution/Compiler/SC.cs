
  /// <summary>
  /// Records a location within a source document
  /// that corresponds to an Abstract Syntax Tree node.
  /// </summary>
  public struct SourceContext
  {
      /// <summary>
      /// The source document within which the AST node is located. 
      /// Null if the node is not derived from a source document.
      /// </summary>
      public Document Document;
    
      /// <summary>
      /// The zero based index of the first character beyond the last character
      /// in the source document that corresponds to the AST node.
      /// </summary>
      public int EndPos;

      /// <summary>
      /// The zero based index of the first character in the source document 
      /// that corresponds to the AST node.
      /// </summary>
      public int StartPos;

      public SourceContext(Document document) : this(document, 0, document.Text.Length)
      {
      }
      
      public SourceContext(Document document, int startPos, int endPos)
      {
          this.Document = document;
          this.StartPos = startPos;
          this.EndPos = endPos;
      }

      public SourceContext(Document document, int startLine, int startColumn, 
                                              int endLine, int endColumn)
      {
          this.Document = document;
          this.Document.GetOffsets(startLine, startColumn, endLine, endColumn, 
                                   out this.StartPos, out this.EndPos);
      }

      /// <summary>
      /// The number (counting from Document.LineNumber) of the line containing 
      /// the first character in the source document that corresponds to the AST node.
      /// </summary>
      public int StartLine
      {
          get 
          {
              if (this.Document == null) return 0;
              return this.Document.GetLine(this.StartPos); 
          }
      }

      /// <summary>
      /// The number (counting from one) of the line column containing the first character
      /// in the source document that corresponds to the AST node.
      /// </summary>
      public int StartColumn
      {
          get
          { 
              if (this.Document == null) return 0;
              return this.Document.GetColumn(this.StartPos); 
          }
      }

      /// <summary>
      /// The number (counting from Document.LineNumber) of the line containing 
      /// the first character beyond the last character in the source document 
      /// that corresponds to the AST node.
      /// </summary>
      public int EndLine
      {
          get
          { 
              if (this.Document == null || this.Document.Text == null) return 0;
              if (this.EndPos >= this.Document.Text.Length) this.EndPos = this.Document.Text.Length;
              return this.Document.GetLine(this.EndPos); 
          }
      }
    
      /// <summary>
      /// The number (counting from one) of the line column containing first character 
      //  beyond the last character in the source document that corresponds to the AST node.
      /// </summary>
      public int EndColumn
      {
          get
          { 
              if (this.Document == null || this.Document.Text == null) return 0;
              if (this.EndPos >= this.Document.Text.Length) this.EndPos = this.Document.Text.Length;
              return this.Document.GetColumn(this.EndPos); 
          }
      }

      /// <summary>
      /// Returns true if the line and column is greater than or equal the position 
      //  of the first character and less than or equal to the position of the last character
      /// of the source document that corresponds to the AST node.
      /// </summary>
      /// <param name="line">A line number(counting from Document.LineNumber)</param>
      /// <param name="column">A column number (counting from one)</param>
      /// <returns></returns>
      public bool Encloses(int line, int column)
      {
          if (line < this.StartLine || line > this.EndLine) return false;
          if (line == this.StartLine) return column >= this.StartColumn;
          if (line == this.EndLine) return column <= this.EndColumn;
          return true;
      }

      public bool Encloses(SourceContext sourceContext)
      {
          return this.StartPos <= sourceContext.StartPos && this.EndPos >= sourceContext.EndPos;
      }

      /// <summary>
      /// The substring of the source document that corresponds to the AST node.
      /// </summary>
      public string SourceText
      {
          get
          {
              if (this.Document == null) return null;
              return this.Document.Substring(this.StartPos, this.EndPos-this.StartPos);
          }
      }
  }
