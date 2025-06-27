//

//
//
using System.Diagnostics;

namespace gpp.builder;


internal interface CharsetExpression
{ }


enum CharsetExpressionOp
{ 
  Append,
  Remove
}

//TODO  Создается исключительно в SelfGrammarParse, из под парсера исключительно + или -
//      Возможно просто два класса сделать SetExpAdd и SetExpSub и метод Evalute()
internal class CharsetExpressionOpBinary(CharsetExpression l_operand_, CharsetExpressionOp operator_, CharsetExpression r_operand_) : CharsetExpression
{
  public readonly CharsetExpression    LeftOperand   = l_operand_;
  public readonly CharsetExpressionOp  Operator      = operator_;
  public readonly CharsetExpression    RightOperand  = r_operand_;
}



public enum CharsetExpressionItemType
{
  Chars,
  Name,
  //TODO  В поле .Text просто набор симболов,
  //      для каждого из которых создается отдельный чарсет и отдельный НФА-путь (.CreateNewEdge)
  //      см. BuilderFA::CreateAutomataItem
  //      ТО ЕСТЬ: в процессе подготовки это можно свести к типу .Chars
  //      ПС. .Name ТАКЖЕ можно свести к типу .Chars в процессе подготовки
  Sequence, 
}

//TODO  Это union-тип. или чарсет или текст
//      поскольку мы унаследованы от CharsetExpression лучше разбить на конкретные типы
//
internal class CharsetExpressionItem : CharsetExpression
{
  private readonly CharsetExpressionItemType  m_Type;
  private readonly string?                    m_Text;
  private BuilderFACharset?                   m_Characters;
  //
  private CharsetExpressionItem(CharsetExpressionItemType type_, string? text_or_null_, BuilderFACharset? charset_or_null_)
  {
    this.m_Type       = type_;
    this.m_Text       = text_or_null_;
    this.m_Characters = charset_or_null_;
  }
  //
  public static CharsetExpressionItem CreateItemCharset(BuilderFACharset charset_)
  {
    return new CharsetExpressionItem(CharsetExpressionItemType.Chars, null, charset_);
  }
  public static CharsetExpressionItem CreateItemName(string name_)
  {
    return new CharsetExpressionItem(CharsetExpressionItemType.Name, name_, null);
  }
  public static CharsetExpressionItem CreateItemSequence(string chars_sequence_text_)
  {
    return new CharsetExpressionItem(CharsetExpressionItemType.Sequence, chars_sequence_text_, null);
  }
  //
  public CharsetExpressionItemType  Type          => this.m_Type;
  public string?                    Text          => this.m_Text;
  public BuilderFACharset?          Characters    => this.m_Characters;
  //
  public void AssignEvalutedNamedCharset(BuilderFACharset charset_)
  { 
    Debug.Assert(m_Type == CharsetExpressionItemType.Name);
    Debug.Assert(m_Characters == null);
    m_Characters = charset_;
  }
}
