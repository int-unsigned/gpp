//
using gpp.parser;
using System.Diagnostics;
//
using GP = gpp.parser;

//
//
namespace gpp.builder;
using static GrammarTables;


#nullable disable




internal sealed class GrammarParser
{
  private GP.Parser       m_parser_engine;
  private GrammarTables   m_grammar_tables;
  //
  private GrammarTables   MyGrammarTables => m_grammar_tables;
  //
  private GrammarParser(GP.ParserTables parser_tables_)
  {
    m_parser_engine = new GP.Parser(parser_tables_);
  }
  public static GrammarParser? Create(AppLog log_, string gpp_egt_tables_file_path_name_)
  {
    GP.ParserTables parser_tables = GP.ParserDbLoader.LoadParserTables(log_, gpp_egt_tables_file_path_name_);
    if (parser_tables == null)
    {
      log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'gp.dat' cannot be loaded.");
      return null;
    }

    string s_gpp_tables_ver = parser_tables.Properties.Version.Trim();
    if (s_gpp_tables_ver.Empty())
    {
      log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'gp.dat' is invalid: ");
      return null;
    }
    else if (s_gpp_tables_ver != BuilderInfo.APP_VERSION_GP_DAT_5_0_1)
    {
      log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'gp.dat' is the incorrect version: '" + s_gpp_tables_ver + "'");
      return null;
    }

    return new GrammarParser(parser_tables);
  }
  //
  public (bool, GrammarTables) DoParse(AppSite app_, PreDefinedCharsetsList builder_predefined_character_sets_, TextReader user_grammar_text_reader_)
  {
    m_grammar_tables = new GrammarTables(app_.Log, builder_predefined_character_sets_);

    m_parser_engine.Open(user_grammar_text_reader_);
    m_parser_engine.TrimReductions = false;
    bool b_parsing_accepted = DoParsingProcedure(app_, m_parser_engine);
    return (b_parsing_accepted, m_grammar_tables);
  }
  //
  private bool DoParsingProcedure(AppSite app_, GP.Parser parser_)
  { 
    //TODO
    AppLog      log_    = app_.Log;
    AppNotify   notify_ = app_.Notify;

    string  s_token_current = "";
    bool    b_done          = false;
    bool    b_accept        = false;

    while (!b_done)
    {
      switch (parser_.Parse())
      {
        case GP.ParseMessage.TokenRead:
          //TODO  А всегда ли .CurrentToken().Data это именно строка? может правильно .ToString()
          //      ...вроде работает... тогда зачем .Data это object ?? разобраться!
          //      ...или при .TokenRead это всегда строка, а далее при продукциях может быть что угодно ??
          s_token_current = (string)parser_.CurrentToken().Data;// Conversions.ToString(parser_.CurrentToken().Data);
          break;
        case GP.ParseMessage.Reduction:
          GP.ParserReduction current_reduction = (GP.ParserReduction)parser_.CurrentReduction;
          object new_reduction_object = this.CreateNewObject(log_, current_reduction);
          parser_.CurrentReduction = new_reduction_object;
          break;
        case GP.ParseMessage.Accept:
          b_accept  = true;
          b_done    = true;          
          break;
        case GP.ParseMessage.NotLoadedError:
          log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "INTERNAL ERROR", "The parser_ was unable to be initialized. Please report this bug.");
          b_done = true;
          break;
        case GP.ParseMessage.LexicalError:
          log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Lexical Error", "Cannot recognize the token after: " + s_token_current, parser_.CurrentPosition().Line);
          b_done = true;
          break;
        case GP.ParseMessage.SyntaxError:
          {

            //TODO  здесь у нас явно вызывается BuilderUtility.DisplayText, но для первого аргумента получают RuntimeHelpers.GetObjectValue(token.Data)
            //      а у .DisplayText две перегрузки - string и CharacterSetBuild
            //      второй аргумент вроде всегда replace_char = false
            //      надеюсь что правильно попал в логику NewLateBinding.LateGet...
            object display_text_obj = parser_.CurrentToken().Data;
            Debug.Assert(display_text_obj != null);
            string s_display_text;
            if(display_text_obj is string)
              s_display_text = BuilderUtility.DisplayText((string)display_text_obj, false);
            else
              s_display_text = BuilderUtility.DisplayText((BuilderFACharset)display_text_obj, false);
            //
            //Type Type = typeof(BuilderUtility);
            //object[] objArray1 = new object[2];
            //object[] objArray2 = objArray1;
            //Token token = parser_.CurrentToken();
            //object objectValue2 = RuntimeHelpers.GetObjectValue(token.Data);
            //objArray2[0] = objectValue2;
            //objArray1[1] = (object)false;
            //object[] objArray3 = objArray1;
            //object[] Arguments = objArray3;
            //bool[] flagArray = new bool[2] { true, false };
            //bool[] CopyBack = flagArray;
            //object Right2 = NewLateBinding.LateGet((object)null, Type, "DisplayText", Arguments, (string[])null, (Type[])null, CopyBack);
            //if (flagArray[0])
            //  token.Data = RuntimeHelpers.GetObjectValue(objArray3[0]);

            string s_friendly_terminals_names_list;
            if (parser_.ExpectedSymbols().Count() > 0)
            {
              s_friendly_terminals_names_list = GrammarParser.FriendlyTerminalName(parser_.ExpectedSymbols()[0]);
              for (int i = 1; i < parser_.ExpectedSymbols().Count(); ++i)
                s_friendly_terminals_names_list += (", " + GrammarParser.FriendlyTerminalName(parser_.ExpectedSymbols()[i]));
            }
            else
              s_friendly_terminals_names_list = "";

            string s_description_read = "Read: " + s_display_text;
            string s_description_expect = "Expecting: " + s_friendly_terminals_names_list;
            string s_description = s_description_read + Environment.NewLine + s_description_expect;
            //
            log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Syntax Error", s_description, parser_.CurrentPosition().Line);

            b_done = true;
            break;
          }
        case GP.ParseMessage.GroupError:
          log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Runaway Comment", "You have a unterminated block comment.");
          b_done = true;
          break;
        case GP.ParseMessage.InternalError:
          log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "INTERNAL ERROR", "The parser_ had an internal error. Please report this bug.");
          b_done = true;
          break;
      }

      notify_.Counter = parser_.CurrentPosition().Line;
    }

    notify_.Counter = parser_.CurrentPosition().Line;
    return b_accept;
  }


  private object CreateNewObject(AppLog log_, GP.ParserReduction the_reduction_)
  {

    Debug.Assert( Enum.IsDefined<ProductionIndex>((ProductionIndex)the_reduction_.Parent.TableIndex) );

    ProductionIndex reduction_id = (ProductionIndex) the_reduction_.Parent.TableIndex;
    
    switch (the_reduction_.Parent.TableIndex)
    {
      case /*13*/ (short)ProductionIndex.Terminalname_Identifier:
        { // <Terminal Name> ::= Identifier
          return GrammarParseHelper.TokenText(the_reduction_.get_Data(0));
        }
      case /*14*/ (short)ProductionIndex.Terminalname_Literal:
        { // <Terminal Name> ::= Literal
          return GrammarParseHelper.TokenText(the_reduction_.get_Data(0));
        }
      case /*15*/ (short)ProductionIndex.Valuelist_Comma: 
        { //                    0           1   2     3
          // <Value List> ::= <Value List> ',' <nlo> <Value Items>
          GrammarValuesList grammar_attr_values_list = (GrammarValuesList)the_reduction_.get_Data(0);
          //<Value Items> - это по ходу всегда строка, возможно из строк разделенных пробелом (см. продукции ниже)
          grammar_attr_values_list.Add((string)the_reduction_.get_Data(3));
          return grammar_attr_values_list;
        }
      case /*16*/ (short)ProductionIndex.Valuelist:
        { // <Value List> ::= <Value Items>
          //<Value Items> - это по ходу всегда строка, возможно из строк разделенных пробелом (см. продукции ниже)
          return new GrammarValuesList((string)the_reduction_.get_Data(0));
        }
      case /*17*/ (short)ProductionIndex.Valueitems:
        { // <Value Items> ::= <Value Items> <Value Item>
          return (the_reduction_.get_Data(0).ToString() + " " + the_reduction_.get_Data(1).ToString());
        }
      case /*18*/ (short)ProductionIndex.Valueitems2:
        { // <Value Items> ::= <Value Item>
          return the_reduction_.get_Data(0);
        }
      case /*19*/ (short)ProductionIndex.Valueitem_Identifier:
        { // <Value Item> ::= Identifier
          return GrammarParseHelper.TokenText(the_reduction_.get_Data(0));
        }
      case /*20*/ (short)ProductionIndex.Valueitem_Nonterminal:
        { // <Value Item> ::= Nonterminal
          return GrammarParseHelper.TokenText(the_reduction_.get_Data(0));
        }
      case /*21*/ (short)ProductionIndex.Valueitem_Literal:
        { // <Value Item> ::= Literal
          return GrammarParseHelper.TokenText(the_reduction_.get_Data(0));
        }
      case /*22*/ (short)ProductionIndex.Param_Parametername_Eq:
        { // <Param> ::= ParameterName <nlo> '=' <Param Body> <nl>
          MyGrammarTables.AddProperty(GrammarParseHelper.TokenText(the_reduction_.get_Data(0)), (GrammarValuesList)the_reduction_.get_Data(3), the_reduction_[0].Position.Line);
        }
        break;
      case /*23*/ (short)ProductionIndex.Parambody_Pipe:
        { // <Param Body> ::= <Param Body> <nlo> '|' <Value List>
          GrammarValuesList grammar_values_list = (GrammarValuesList)the_reduction_.get_Data(0);
          grammar_values_list.Add((GrammarValuesList)the_reduction_.get_Data(3));
          return grammar_values_list;
        }
      case /*24*/ (short)ProductionIndex.Parambody:
        { // <Param Body> ::= <Value List>
          return the_reduction_.get_Data(0);
        }
      case /*25*/ (short)ProductionIndex.Attributedecl_Ateq_Lbrace_Rbrace:
        { // <Attribute Decl> ::= <Terminal Name> <nlo> '@=' '{' <Attribute List> '}' <nl>
          //TODO  Неизвестная для меня грамматическая конструкция...
          MyGrammarTables.AddSymbolAttrib(new GrammarAttribute(the_reduction_.get_Data(0).ToString(), (GrammarAttrItemsList)the_reduction_.get_Data(4), the_reduction_[2].Position.Line));
        }
        break;
      case /*26*/ (short)ProductionIndex.Attributedecl_Identifier_Ateq_Lbrace_Rbrace:
        { // <Attribute Decl> ::= <Terminal Name> Identifier <nlo> '@=' '{' <Attribute List> '}' <nl>
          //TODO  Неизвестная для меня грамматическая конструкция...
          string attrib_assign_name = the_reduction_.get_Data(0).ToString() + " " + the_reduction_.get_Data(1).ToString();
          MyGrammarTables.AddGroupAttrib(new GrammarAttribute(attrib_assign_name, (GrammarAttrItemsList)the_reduction_.get_Data(5), the_reduction_[3].Position.Line));
        }
        break;
      case /*27*/ (short)ProductionIndex.Attributelist_Comma:
        { // <Attribute List> ::= <Attribute List> ',' <nlo> <Attribute Item>
          GrammarAttrItemsList grammar_attr_list = (GrammarAttrItemsList)the_reduction_.get_Data(0);
          grammar_attr_list.Add((GrammarAttrItem)the_reduction_.get_Data(3));
          return grammar_attr_list;
        }
      case /*28*/ (short)ProductionIndex.Attributelist:
        { // <Attribute List> ::= <Attribute Item>
          //TODO  Какая то непонятная для меня конструкция c# была...
          //      понимаю так, что просто нужно создать новый GrammarAttrList с переданным элементом <Attribute Item> который имеет тип SelfGrammarAttr
          return new GrammarAttrItemsList((GrammarAttrItem)the_reduction_.get_Data(0));
        }
      case /*29*/ (short)ProductionIndex.Attributeitem_Identifier_Eq_Identifier:
        { // <Attribute Item> ::= Identifier '=' Identifier
          //TODO  Полагаю, что ...'=' Identifier - это строка. Надо посмотреть по грамматике что есть Identifier := ...
          return new GrammarAttrItem(the_reduction_.get_Data(0).ToString(), the_reduction_.get_Data(2).ToString(), is_set_: false);

          //SelfGrammarAttr new_grammar_attr = new SelfGrammarAttr();
          //new_grammar_attr.Name = Conversions.ToString(the_reduction_.get_Data(0));
          //GrammarAttrValuesList new_grammar_attr_list = new_grammar_attr.List;

          //object attr_value_object = the_reduction_.get_Data(2);
          //Debug.Assert(attr_value_object is string);
          //new_grammar_attr_list.Add((string) attr_value_object);
          //new_grammar_attr.IsSet = false;
          //return new_grammar_attr;

          //object[] objArray7 = new object[1];
          //object[] objArray8 = objArray7;
          ////Reduction reduction6 = the_reduction_;
          ////Reduction reduction7 = reduction6;
          ////int Index5 = 2;
          ////int Index6 = Index5;
          //object objectValue3 = RuntimeHelpers.GetObjectValue(the_reduction_.get_Data(2));
          //objArray8[0] = objectValue3;
          //object[] objArray9 = objArray7;
          //object[] Arguments3 = objArray9;
          //bool[] flagArray3 = new bool[1] { true };
          //bool[] CopyBack3 = flagArray3;
          //NewLateBinding.LateCall((object)new_grammar_attr_list, (Type)null, "Add", Arguments3, (string[])null, (Type[])null, CopyBack3, true);
          //if (flagArray3[0])
          //  the_reduction_.set_Data(2, RuntimeHelpers.GetObjectValue(objArray9[0]));

          //new_grammar_attr.IsSet = false;
          //return new_grammar_attr;
        }
      case /*30*/ (short)ProductionIndex.Attributeitem_Identifier_Eq_Lbrace_Rbrace:
        { // <Attribute Item> ::= Identifier '=' '{' <Value List> '}'
          return new GrammarAttrItem(the_reduction_.get_Data(0).ToString(), (GrammarValuesList)the_reduction_.get_Data(3), true);
        }
      case /*31*/ (short)ProductionIndex.Setdecl_Lbrace_Rbrace_Eq:
        { // <Set Decl> ::= '{' <ID Series> '}' <nlo> '=' <Set Exp> <nl>
          GrammarTables.GrammarSet charset = new GrammarTables.GrammarSet(the_reduction_.get_Data(1).ToString(), (CharsetExpression)the_reduction_.get_Data(5), the_reduction_[0].Position.Line);
          MyGrammarTables.AddUserSet(charset);
        }
        break;
      case /*32*/ (short)ProductionIndex.Setexp_Plus:
        { // <Set Exp> ::= <Set Exp> <nlo> '+' <Set Item>
          return new CharsetExpressionOpBinary((CharsetExpression)the_reduction_.get_Data(0), CharsetExpressionOp.Append, (CharsetExpression)the_reduction_.get_Data(3));
        }
      case /*33*/ (short)ProductionIndex.Setexp_Minus:
        { // <Set Exp> ::= <Set Exp> <nlo> '-' <Set Item>
          return new CharsetExpressionOpBinary((CharsetExpression)the_reduction_.get_Data(0), CharsetExpressionOp.Remove, (CharsetExpression)the_reduction_.get_Data(3));
        }
      case /*34*/ (short)ProductionIndex.Setexp:
        { // <Set Exp> ::= <Set Item>
          return the_reduction_.get_Data(0);
        }
      case /*35*/ (short)ProductionIndex.Setitem_Setliteral:
        { // <Set Item> ::= SetLiteral
          //TODO  вроде бы set_literal_full_obj всегда объект string, но мы применяем метод .ToString()
          //      может быть просто достаточно (string) ??
          object set_literal_full_obj = the_reduction_.get_Data(0);
          string set_literal_full_text = set_literal_full_obj.ToString();
          GrammarParseHelper.WarnRegexSetLiteral(log_, set_literal_full_text, the_reduction_[0].Position.Line);
          the_reduction_.set_Data(0, set_literal_full_text);
          return CharsetExpressionItem.CreateItemCharset(new BuilderFACharset(GrammarParseHelper.TokenText(set_literal_full_text)));
        }
      case /*36*/ (short)ProductionIndex.Setitem_Lbrace_Rbrace:
        { // <Set Item> ::= '{' <ID Series> '}'
          //TODO  .AddUsedSetName создает новый объект GrammarIdentifier, но мы возвращаем новый CharsetExpressionItem
          //      надо бы разобраться с этой механикой...
          string name = the_reduction_.get_Data(1).ToString();
          MyGrammarTables.AddUsedSetName(name, the_reduction_[0].Position.Line);
          return CharsetExpressionItem.CreateItemName(name);
        }
      case /*37*/ (short)ProductionIndex.Setitem_Lbrace_Rbrace2:
        { // <Set Item> ::= '{' <Charcode List> '}'
          //NOTE  продукции <Charcode List> ::=... все возвращают CharacterSetBuild
          return CharsetExpressionItem.CreateItemCharset((BuilderFACharset)the_reduction_.get_Data(1));
        }
      case /*38*/ (short)ProductionIndex.Idseries_Identifier:
        { // <ID Series> ::= <ID Series> Identifier
          return the_reduction_.get_Data(0).ToString() + " " + the_reduction_.get_Data(1).ToString();
        }
      case /*39*/ (short)ProductionIndex.Idseries_Identifier2:
        { // <ID Series> ::= Identifier
          return the_reduction_.get_Data(0);
        }
      case /*40*/ (short)ProductionIndex.Charcodelist_Comma:
        { // <Charcode List> ::= <Charcode List> ',' <nlo> <Charcode Item>
          BuilderFACharset charset   = (BuilderFACharset)the_reduction_.get_Data(0);
          GrammarCharRange  charrange = (GrammarCharRange)the_reduction_.get_Data(3);
          charset.AddRange(charrange.char_begin, charrange.char_final);
          return charset;
        }
      case /*41*/ (short)ProductionIndex.Charcodelist:
        { // <Charcode List> ::= <Charcode Item>
          GrammarCharRange charrange = (GrammarCharRange)the_reduction_.get_Data(0);
          return new BuilderFACharset(charrange.char_begin, charrange.char_final);
        }
      case /*42*/ (short)ProductionIndex.Charcodeitem:
        { // <Charcode Item> ::= <Charcode Value>
          return new GrammarCharRange(to_char((int)the_reduction_.get_Data(0)), to_char((int)the_reduction_.get_Data(0)));
        }
      case /*43*/ (short)ProductionIndex.Charcodeitem_Dotdot:
        { // <Charcode Item> ::= <Charcode Value> '..' <Charcode Value>
          //
          //NOTE  <Charcode Value> - это обработанное парсером продукция .Charcodevalue_Hexnumber или .Charcodevalue_Decnumber
          //      они возвращают int charcode_value
          //      поэтому здесь нам достаточно просто преобразовать object .get_Data(0) и .get_Data(2) в int
          return new GrammarCharRange(to_char((int)the_reduction_.get_Data(0)), to_char((int)the_reduction_.get_Data(2)));
        }
      case /*44*/ (short)ProductionIndex.Charcodevalue_Hexnumber:
        { // <Charcode Value> ::= HexNumber
          string charcode_text = the_reduction_.get_Data(0).ToString();
          if (GrammarParseHelper.GetCharcodeFromHex(charcode_text, out int charcode_value))
            return charcode_value;
          //else
          log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Invalid set constant value", "The value '" + charcode_text + "' is not valid.", the_reduction_[0].Position.Line);
          return null;
        }
      case /*45*/ (short)ProductionIndex.Charcodevalue_Decnumber:
        { // <Charcode Value> ::= DecNumber
          string charcode_text = the_reduction_.get_Data(0).ToString();
          if (GrammarParseHelper.GetCharcodeFromDec(charcode_text, out int charcode_value))
            return charcode_value;
          //else
          log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Invalid set constant value", "The value '" + charcode_text + "' is not valid.", the_reduction_[0].Position.Line);
          return null;
        }
      case /*46*/ (short)ProductionIndex.Groupdecl_Identifier_Eq:
        { // <Group Decl> ::= <Terminal Name> Identifier <nlo> '=' <Group Item> <nl>
          const string s_LINE   = "LINE";
          const string s_START  = "START";
          const string s_END    = "END";
          string group_usage    = the_reduction_.get_Data(1).ToString();
          string GROUP_USAGE    = group_usage.ToUpper();
          if (GROUP_USAGE == s_LINE || GROUP_USAGE == s_START || GROUP_USAGE == s_END)
            MyGrammarTables.AddGroupOrUpdateExisting(new GrammarTables.GrammarGroup(the_reduction_.get_Data(0).ToString(), group_usage, the_reduction_.get_Data(4).ToString(), the_reduction_[1].Position.Line));
          else
            log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Invalid group usage value", "The usage value '" + group_usage + "' is not valid. It can be either Start, End or Line.", the_reduction_[1].Position.Line);
        }
        break;
      case /*47*/ (short)ProductionIndex.Groupitem_Identifier:
        { // <Group Item> ::= Identifier
          return GrammarParseHelper.TokenText(the_reduction_.get_Data(0));
        }
      case /*48*/ (short)ProductionIndex.Groupitem_Literal:
        { // <Group Item> ::= Literal
          return GrammarParseHelper.TokenText(the_reduction_.get_Data(0));
        }
      case /*49*/ (short)ProductionIndex.Terminaldecl_Eq:
        { // <Terminal Decl> ::= <Terminal Name> <nlo> '=' <Terminal Body> <nl>
          string                      symb_name           = the_reduction_.get_Data(0).ToString();
          TerminalExpression          symb_terminal_expr  = (TerminalExpression)the_reduction_.get_Data(3);
          int                         symb_line           = the_reduction_[2].Position.Line;
          GrammarTables.GrammarSymbol symb                = new GrammarTables.GrammarSymbol(symb_name, SymbolType.Content, symb_line, symb_terminal_expr);
          MyGrammarTables.AddTerminalHead(symb);
        }
        break;
      case /*50*/ (short)ProductionIndex.Terminalbody_Pipe:
        { // <Terminal Body> ::= <Terminal Body> <nlo> '|' <Reg Exp Seq>
          TerminalExpression          regexp     = the_reduction_.Data<TerminalExpression>(0);
          TerminalExpressionSequence regexp_seq  = (TerminalExpressionSequence)the_reduction_.get_Data(3);
          regexp_seq.Priority   = (short)-1;
          regexp.Add(regexp_seq);
          return regexp;
        }
      case /*51*/ (short)ProductionIndex.Terminalbody:
        { // <Terminal Body> ::= <Reg Exp Seq>
          TerminalExpression          regexp      = new TerminalExpression();
          TerminalExpressionSequence  regexp_seq  = (TerminalExpressionSequence)the_reduction_.get_Data(0);
          regexp_seq.Priority   = (short)-1;
          regexp.Add(regexp_seq);
          return regexp;
        }
      case /*52*/ (short)ProductionIndex.Regexpseq:
        { // <Reg Exp Seq> ::= <Reg Exp Seq> <Reg Exp Item>
          TerminalExpressionSequence regexp_seq  = (TerminalExpressionSequence)the_reduction_.get_Data(0);
          TerminalExpressionItem  regexp_item = (TerminalExpressionItem)the_reduction_.get_Data(1);
          regexp_seq.Add(regexp_item);
          the_reduction_.set_Data(1, (object)regexp_item);
          return regexp_seq;
        }
      case /*53*/ (short)ProductionIndex.Regexpseq2:
        { // <Reg Exp Seq> ::= <Reg Exp Item>
          //TODO  И здесь и выше много создается new RegExpItemsSequence() и сразу в него добавляется RegExpItem - нужно создавать новый сразу с RegExpItem
          TerminalExpressionSequence regexp_seq  = new TerminalExpressionSequence();
          TerminalExpressionItem     regexp_item = (TerminalExpressionItem)the_reduction_.get_Data(0);
          regexp_seq.Add(regexp_item);
          the_reduction_.set_Data(0, (object)regexp_item);
          return regexp_seq;
        }
      case /*54*/ (short)ProductionIndex.Regexpitem:
        { // <Reg Exp Item> ::= <Set Item> <Kleene Opt>
          return new TerminalExpressionItem((CharsetExpressionItem)the_reduction_.get_Data(0), KleeneOpFromString(the_reduction_.get_Data(1).ToString()));
        }
      case /*55*/ (short)ProductionIndex.Regexpitem_Literal:
        { // <Reg Exp Item> ::= Literal <Kleene Opt>
          //TODO  Оригинальная грамматика определяет Literal так
          //      Literal 		= '' {Literal Ch}* ''
          //      то есть возможны просто пустые кавычки (''), и после .TokenText мы получим пустую строку
          //      и далее у нас как минимум в BuilderFA::CreateAutomataItem алгоритм поломается (вроде..)
          //      так как он полагает, что хоть один символ в .Text есть (вроде..)
          //TODO  Видимо именно здесь нужно делать критическую ОШ в лог, т.к. во всех остальных случаях
          //      создание "пустой" CreateItemSequence прикрыто парсером (вроде..)
          //      .. будем посмотреть пока на .Assert и поведение. тестировать надо..
          string sequence_text = GrammarParseHelper.TokenText(the_reduction_.get_Data(0));
          Debug.Assert(sequence_text.Length > 0);
          CharsetExpressionItem item = CharsetExpressionItem.CreateItemSequence(sequence_text);
          return new TerminalExpressionItem(item, KleeneOpFromString(the_reduction_.get_Data(1).ToString()));
        }
      case /*56*/ (short)ProductionIndex.Regexpitem_Identifier:
        { // <Reg Exp Item> ::= Identifier <Kleene Opt>
          CharsetExpressionItem item = CharsetExpressionItem.CreateItemSequence(the_reduction_.get_Data(0).ToString());
          return new TerminalExpressionItem(item, KleeneOpFromString(the_reduction_.get_Data(1).ToString()));
        }
      case /*57*/ (short)ProductionIndex.Regexpitem_Lparan_Rparan:
        { // <Reg Exp Item> ::= '(' <Sub Reg Exp> ')' <Kleene Opt>
          return new TerminalExpressionItem((TerminalExpression)the_reduction_.get_Data(1), KleeneOpFromString( the_reduction_.get_Data(3).ToString() ));
        }
      case /*58*/ (short)ProductionIndex.Subregexp_Pipe:
        { // <Sub Reg Exp> ::= <Sub Reg Exp> '|' <Reg Exp Seq>
          TerminalExpression    regexp      = (TerminalExpression)the_reduction_.get_Data(0);
          TerminalExpressionSequence regexp_seq  = (TerminalExpressionSequence)the_reduction_.get_Data(2);
          regexp.Add(regexp_seq);
          the_reduction_.set_Data(2, (object)regexp_seq);
          return regexp;
        }
      case /*59*/ (short)ProductionIndex.Subregexp:
        { // <Sub Reg Exp> ::= <Reg Exp Seq>
          TerminalExpression regexp      = new TerminalExpression();
          TerminalExpressionSequence regexp_seq  = (TerminalExpressionSequence)the_reduction_.get_Data(0);
          regexp.Add(regexp_seq);
          the_reduction_.set_Data(0, (object)regexp_seq);
          return regexp;
        }
      case /*60*/ (short)ProductionIndex.Kleeneopt_Plus:
        { // <Kleene Opt> ::= '+'
          return "+";
        }
      case /*61*/ (short)ProductionIndex.Kleeneopt_Question:
        { // <Kleene Opt> ::= '?'
          return "?";
        }
      case /*62*/ (short)ProductionIndex.Kleeneopt_Times:
        { // <Kleene Opt> ::= '*'
          return "*";
        }
      case /*63*/ (short)ProductionIndex.Kleeneopt:
        { // <Kleene Opt> ::=
          return "";
        }
      case /*64*/ (short)ProductionIndex.Ruledecl_Nonterminal_Coloncoloneq:
        { // <Rule Decl> ::= Nonterminal <nlo> '::=' <Handles> <nl>
          GrammarTables.GrammarProductionList reduction_productions_list = (GrammarTables.GrammarProductionList)the_reduction_.get_Data(3);
          string        symb_name = GrammarParseHelper.TokenText(the_reduction_.get_Data(0));
          int           symb_line = the_reduction_[2].Position.Line;
          GrammarSymbol symb      = new GrammarTables.GrammarSymbol(symb_name, SymbolType.Nonterminal, symb_line);
          foreach (GrammarTables.GrammarProduction production in reduction_productions_list)
          {
            production.Head = symb;
            MyGrammarTables.AddProduction(production);
          }          
        }
        break;
      case /*65*/ (short)ProductionIndex.Handles_Pipe:
        { // <Handles> ::= <Handles> <nlo> '|' <Handle>
          GrammarTables.GrammarProductionList productions_list = (GrammarTables.GrammarProductionList)the_reduction_.get_Data(0);
          productions_list.Add(new GrammarTables.GrammarProduction((GrammarTables.GrammarSymbolList)the_reduction_.get_Data(3), the_reduction_[2].Position.Line));
          return productions_list;
        }
      case /*66*/ (short)ProductionIndex.Handles:
        { // <Handles> ::= <Handle>
          return new GrammarTables.GrammarProductionList(new GrammarTables.GrammarProduction((GrammarTables.GrammarSymbolList)the_reduction_.get_Data(0), the_reduction_[0].Position.Line));
        }
      case /*67*/ (short)ProductionIndex.Handle:
        { // <Handle> ::= <Symbols>
          return (GrammarTables.GrammarSymbolList)the_reduction_.get_Data(0);
        }
      case /*68*/ (short)ProductionIndex.Handle_Ltgt:
        { // <Handle> ::= '<>'
          return new GrammarTables.GrammarSymbolList();
        }
      case /*69*/ (short)ProductionIndex.Symbols:
        { // <Symbols> ::= <Symbols> <Symbol>
          GrammarTables.GrammarSymbolList grammar_symbol_list = (GrammarTables.GrammarSymbolList)the_reduction_.get_Data(0);
          grammar_symbol_list.Add((GrammarTables.GrammarSymbol)the_reduction_.get_Data(1));
          return grammar_symbol_list;
        }
      case /*70*/ (short)ProductionIndex.Symbols2:
        { // <Symbols> ::=
          return new GrammarTables.GrammarSymbolList();
        }
      case /*71*/ (short)ProductionIndex.Symbol:
        { // <Symbol> ::= <Terminal Name>
          GrammarTables.GrammarSymbol symb = new GrammarTables.GrammarSymbol(the_reduction_.get_Data(0).ToString(), SymbolType.Content, the_reduction_[0].Position.Line);
          MyGrammarTables.AddHandleSymbol(symb);
          return symb;
        }
      case /*72*/ (short)ProductionIndex.Symbol_Nonterminal:
        { // <Symbol> ::= Nonterminal
          GrammarTables.GrammarSymbol symb = new GrammarTables.GrammarSymbol(GrammarParseHelper.TokenText(the_reduction_.get_Data(0)), SymbolType.Nonterminal, the_reduction_[0].Position.Line);
          MyGrammarTables.AddHandleSymbol(symb);
          return symb;
        }
    }

    return null;
  }


  private static string FriendlyTerminalName(GP.ParserSymbol Sym)
  {
    string str;
    switch ((short) ((int) Sym.TableIndex - 22))
    {
      case 0:
        str = "Decimal Number";
        break;
      case 1:
        str = "Hexadecimal Number";
        break;
      case 2:
        str = "Identifier";
        break;
      case 3:
        str = "Literal";
        break;
      case 4:
        str = "New Line";
        break;
      case 5:
        str = "Nonterminal";
        break;
      case 6:
        str = "Parameter Name";
        break;
      case 7:
        str = "Set Literal";
        break;
      default:
        str = Sym.Name;
        break;
    }
    return str;
  }


  private enum SymbolIndex
  {
    Eof,
    Error,
    Comment,
    Whitespace,
    Exclam,
    Exclamtimes,
    Timesexclam,
    Minus,
    Lparan,
    Rparan,
    Times,
    Comma,
    Dotdot,
    Coloncoloneq,
    Question,
    Ateq,
    Lbrace,
    Pipe,
    Rbrace,
    Plus,
    Ltgt,
    Eq,
    Decnumber,
    Hexnumber,
    Identifier,
    Literal,
    Newline,
    Nonterminal,
    Parametername,
    Setliteral,
    Attributedecl,
    Attributeitem,
    Attributelist,
    Charcodeitem,
    Charcodelist,
    Charcodevalue,
    Content,
    Definition,
    Grammar,
    Groupdecl,
    Groupitem,
    Handle,
    Handles,
    Idseries,
    Kleeneopt,
    Nl,
    Nlo,
    Param,
    Parambody,
    Regexpitem,
    Regexpseq,
    Ruledecl,
    Setdecl,
    Setexp,
    Setitem,
    Subregexp,
    Symbol,
    Symbols,
    Terminalbody,
    Terminaldecl,
    Terminalname,
    Valueitem,
    Valueitems,
    Valuelist,
  }

  
  private enum ProductionIndex : short
  {
    Grammar,
    Content,
    Content2,
    Definition,
    Definition2,
    Definition3,
    Definition4,
    Definition5,
    Definition6,
    Nlo_Newline,
    Nlo,
    Nl_Newline,
    Nl_Newline2,
    Terminalname_Identifier,
    Terminalname_Literal,
    Valuelist_Comma,
    Valuelist,
    Valueitems,
    Valueitems2,
    Valueitem_Identifier,
    Valueitem_Nonterminal,
    Valueitem_Literal,
    Param_Parametername_Eq,
    Parambody_Pipe,
    Parambody,
    Attributedecl_Ateq_Lbrace_Rbrace,
    Attributedecl_Identifier_Ateq_Lbrace_Rbrace,
    Attributelist_Comma,
    Attributelist,
    Attributeitem_Identifier_Eq_Identifier,
    Attributeitem_Identifier_Eq_Lbrace_Rbrace,
    Setdecl_Lbrace_Rbrace_Eq,
    Setexp_Plus,
    Setexp_Minus,
    Setexp,
    Setitem_Setliteral,
    Setitem_Lbrace_Rbrace,
    Setitem_Lbrace_Rbrace2,
    Idseries_Identifier,
    Idseries_Identifier2,
    Charcodelist_Comma,
    Charcodelist,
    Charcodeitem,
    Charcodeitem_Dotdot,
    Charcodevalue_Hexnumber,
    Charcodevalue_Decnumber,
    Groupdecl_Identifier_Eq,
    Groupitem_Identifier,
    Groupitem_Literal,
    Terminaldecl_Eq,
    Terminalbody_Pipe,
    Terminalbody,
    Regexpseq,
    Regexpseq2,
    Regexpitem,
    Regexpitem_Literal,
    Regexpitem_Identifier,
    Regexpitem_Lparan_Rparan,
    Subregexp_Pipe,
    Subregexp,
    Kleeneopt_Plus,
    Kleeneopt_Question,
    Kleeneopt_Times,
    Kleeneopt,
    Ruledecl_Nonterminal_Coloncoloneq,
    Handles_Pipe,
    Handles,
    Handle,
    Handle_Ltgt,
    Symbols,
    Symbols2,
    Symbol,
    Symbol_Nonterminal,
  }
}
