//
using System.Diagnostics;

//
//
namespace gpp.builder;


internal class TerminalExpressionItem
{
  private object    m_Data;
  private KleeneOp  m_KleeneOp;
  //
  public TerminalExpressionItem(TerminalExpression data_, KleeneOp kleene_op_)
  {
    this.m_Data     = data_;
    this.m_KleeneOp = kleene_op_;
  }
  public TerminalExpressionItem(CharsetExpressionItem data_, KleeneOp kleene_op_)
  {
    this.m_Data     = data_;
    this.m_KleeneOp = kleene_op_;
  }
  //
  public object Data        => m_Data;
  public KleeneOp KleeneOp  => m_KleeneOp;
  //
  public bool IsVariableLength()
  {        
    return ( m_KleeneOp == KleeneOp.One_Or_More 
            || m_KleeneOp == KleeneOp.Zero_Or_More 
            || ((m_Data is TerminalExpression data_as_regexp) && data_as_regexp.IsVariableLength()));
  }
}



internal class TerminalExpressionSequence
{
  private List<TerminalExpressionItem>  m_Items;
  private short                         m_Priority;
  //
  public TerminalExpressionSequence()             => this.m_Items = new List<TerminalExpressionItem>();
  //
  public int Count()                              => this.m_Items.Count;
  public void Add(TerminalExpressionItem item_)   => this.m_Items.Add(item_);
  public TerminalExpressionItem this[int index_]
  {
    get
    { //TODO  в оригинале нулл возвращали. непонятно зачем и вообще это моветон какой-то..
      Debug.Assert(index_ >= 0 && index_ < this.m_Items.Count);
      return this.m_Items[index_];
    }
  }
  public bool IsVariableLength()
  {
    foreach(TerminalExpressionItem item in this.m_Items)
      if(item.IsVariableLength()) 
        return true;
    return false;
  }
  internal short Priority
  {
    get => this.m_Priority;
    set => this.m_Priority = value;
  }

  public override string ToString()
  {
    return this.m_Items.ToStringDelimited(" ");
  }
}



internal class TerminalExpression
{
  private List<TerminalExpressionSequence> m_Array;
  //
  public TerminalExpression() => this.m_Array         = new List<TerminalExpressionSequence>();
  //
  public int Count()                                  => this.m_Array.Count;
  public void Add(TerminalExpressionSequence item_)   => this.m_Array.Add(item_);
  public TerminalExpressionSequence this[int index_]
  {
    get
    { //TODO  в оригинале нулл возвращали. непонятно зачем и вообще это моветон какой-то..
      Debug.Assert(index_ >= 0 && index_ < this.m_Array.Count);
      return this.m_Array[index_];
    }  
  }
  public bool IsVariableLength()
  {
    foreach (TerminalExpressionSequence seq in this.m_Array)
      if (seq.IsVariableLength())
        return true;
    return false;
  }
  public override string ToString()
  {
    return m_Array.ToStringDelimited(" | ");
  }

  //TODO  Хреновая функция. Вызывается только один раз с фиксированной строкой
  //      Exp.AddTextExpr("{LF}|{CR}{LF}?|{LS}|{PS}");
  //      а если передать что-то <другое> - результат может быть непредсказуем
  public void AddTextExpr(string expression_text_)
  {
    const string s_PLUS = "+";
    const string s_STAR = "*";
    const string s_QUES = "?";

    string[] expression_parts_array = expression_text_.Split('|', StringSplitOptions.TrimEntries);

    //TODO  в оригинале переменная была здесь, за циклом - возможно это ОШ, а возможно что-то было задумано
    //      Разобраться надо!
    string expression_part_body = "";
  
    for (int expression_part_index = 0; expression_part_index < expression_parts_array.Length; ++expression_part_index)
    {
      string                      expression_part     = expression_parts_array[expression_part_index];
      int                         expression_part_len = expression_part.Length;
      TerminalExpressionSequence  regexp_seq          = new TerminalExpressionSequence();
      int                         char_index          = 0;      
      while (char_index < expression_part_len)
      {
        if (expression_part[char_index] == '{')
        {
          int index_of_bra_last = expression_part.IndexOf("}", char_index);
          //TODO  В оригинале не проверяли случай если закрывающей скобки нет.
          //      Впрочем этот метод вызывается только один раз с фиксированным текстом - и зачем он тогда?
          //      Задел на будущее?
          Debug.Assert(index_of_bra_last > char_index);
          expression_part_body = expression_part.Substring(char_index + 1, index_of_bra_last - char_index - 1);
          char_index = index_of_bra_last + 1;
        }
        //TODO  Здесь если строка вдруг НЕ начинается с {, то мы провалимся вниз и добавим expression_part_body
        //      ОСТАВШУЮСЯ с предыдущего expression_part - это так задумано? или .. что-то тут хотели, но недодумали походу

        string kleene_op = "";
        if (char_index < expression_part_len)
        {
          string next_char = expression_part.Substring(char_index, 1);
          if (next_char == s_PLUS || next_char == s_QUES || next_char == s_STAR)
          {
            kleene_op = next_char;  //TODO  Если уж на то пошло, то kleene_op char делать надо, а не строкой!
            ++char_index;
          }
          //TODO  else ?? - А если строка не закончилась, но next_char непонятночто?
        }

        TerminalExpressionItem regexp_item = new TerminalExpressionItem(CharsetExpressionItem.CreateItemName(expression_part_body), KleeneOpFromString(kleene_op));
        regexp_seq.Add(regexp_item);
      }

      this.m_Array.Add(regexp_seq);
    }
  }
}
