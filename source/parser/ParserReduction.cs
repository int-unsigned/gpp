//

//
//
namespace gpp.parser;


public class ParserReduction 
{
  private   ParserProduction  m_Parent;
  protected ParserToken[]     m_Tokens;
  private   object?     m_Tag;
  //
  protected ParserReduction(ParserProduction production_, int production_handle_count_) 
  { 
    m_Parent  = production_;
    m_Tokens  = new ParserToken[production_handle_count_];
    m_Tag     = null;
  }
  //
  public ParserProduction Parent      => this.m_Parent;
  public ParserToken this[int index_] => m_Tokens[index_];
  public object? Tag
  {
    get => this.m_Tag;
    set => this.m_Tag = value;
  }

  public object?  get_Data(int index_) => m_Tokens[index_].Data;
  public void     set_Data(int index_, object value_)
  {
    m_Tokens[index_].Data = value_;
  }

  //TODO
  public T Data<T>(int index_)
  {
    return (T)m_Tokens[index_].Data;
  }
}


public class ParserReductionConstructor(ParserProduction production_, int production_handle_count_) : ParserReduction(production_, production_handle_count_)
{
  public void SetTokenAtIndex(int index_, ParserToken token_)
  {
    base.m_Tokens[index_] = token_;
  }
}