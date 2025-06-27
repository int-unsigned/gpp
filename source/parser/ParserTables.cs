//

//
//
namespace gpp.parser;


internal class ParserTables
{
  protected ParserProperties        m_Properties;
  protected ParserSymbolList        m_Symbol;
  protected ParserCharsetsList      m_CharSet;
  protected ParserGroupList         m_Group;
  protected ParserFAStatesList      m_DFA;
  protected ParserProductionList    m_Production;
  protected ParserLRStateList       m_LALR;
  //
  protected ParserTables(ParserProperties properties_, ParserSymbolList symbols_, ParserGroupList groups_, ParserProductionList productions_,
                        ParserFAStatesList fa_, ParserCharsetsList charsets_, ParserLRStateList lr_)
  { 
    m_Properties  = properties_;
    m_Symbol      = symbols_;
    m_Group       = groups_;
    m_Production  = productions_;
    m_DFA         = fa_;
    m_CharSet     = charsets_;
    m_LALR        = lr_;
  }


  public bool IsLoaded() => (!this.m_DFA.IsEmpty() && !this.m_LALR.IsEmpty());

  public ParserProperties       Properties  {    get => this.m_Properties;  }
  public ParserSymbolList       Symbol      {    get => this.m_Symbol;  }
  public ParserCharsetsList     CharSet     {    get => this.m_CharSet;  }
  public ParserProductionList   Production  {    get => this.m_Production;  }
  public ParserFAStatesList     DFA         {    get => this.m_DFA;  }
  public ParserLRStateList      LALR        {    get => this.m_LALR;  }
  public ParserGroupList        Group       {    get => this.m_Group;  }
}
