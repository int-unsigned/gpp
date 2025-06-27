//
using System.Diagnostics;

//
//
namespace gpp.builder;


internal class BuilderTablesBase 
{
  protected BuilderPropertiesList     m_properties;
  protected BuilderSymbolsList           m_Symbol;
  protected BuilderGroupsList            m_Group;
  protected BuilderProductionsList       m_Production;
  //
  protected BuilderTablesBase(BuilderPropertiesList properties_, BuilderSymbolsList symbols_, BuilderGroupsList groups_, BuilderProductionsList productions_)
  {
    this.m_properties       = properties_;
    this.m_Symbol           = symbols_;
    this.m_Group            = groups_;
    this.m_Production       = productions_;    
  }
  public BuilderPropertiesList      Properties      => this.m_properties; 
  public BuilderSymbolsList         Symbol          => (BuilderSymbolsList)this.m_Symbol; 
  public BuilderProductionsList     Production      => (BuilderProductionsList)this.m_Production; 
  public BuilderGroupsList          Group           => (BuilderGroupsList)this.m_Group; 
}


internal class BuilderTables : BuilderTablesBase
{
  protected UserDefinedCharsetsList   m_UserDefinedSets;
  private UnicodeTable                m_PredefinedUnicodeTable;
  //
  public BuilderTables(BuilderPropertiesList properties_, BuilderSymbolsList symbols_, BuilderGroupsList groups_, BuilderProductionsList productions_,
    UnicodeTable predefined_unicode_table_, UserDefinedCharsetsList user_defined_charsets_)
    :base(properties_, symbols_, groups_, productions_)
  {
    this.m_PredefinedUnicodeTable = predefined_unicode_table_;
    this.m_UserDefinedSets        = user_defined_charsets_;
  }
  //
  public UnicodeTable             PredefinedUnicodeTable  => m_PredefinedUnicodeTable;
  public UserDefinedCharsetsList  UserDefinedSets         => m_UserDefinedSets;
}



internal class BuilderTablesView : BuilderTablesBase
{
  protected BuilderLRStatesList        m_LALR;
  protected BuilderFAStatesList        m_DFA;
  protected BuilderFACharsetsList   m_Charsets;
  //
  protected BuilderTablesView(BuilderPropertiesList properties_, BuilderSymbolsList symbols_, BuilderGroupsList groups_, BuilderProductionsList productions_, 
    BuilderLRStatesList lr_, BuilderFAStatesList fa_, BuilderFACharsetsList charsets_) : base(properties_, symbols_, groups_, productions_)
  {
    this.m_LALR = lr_;
    this.m_DFA = fa_;
    this.m_Charsets = charsets_;
  }
  //
  public BuilderFAStatesList       DFA       => (BuilderFAStatesList)this.m_DFA;    
  public BuilderLRStatesList       LALR      => (BuilderLRStatesList)this.m_LALR;
  public BuilderFACharsetsList  Charsets  => this.m_Charsets;
}



