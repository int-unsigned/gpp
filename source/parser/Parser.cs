//
using System.Diagnostics;

//
//
namespace gpp.parser;


public enum ParseMessage
{
  TokenRead,
  Reduction,
  Accept,
  NotLoadedError,
  LexicalError,
  SyntaxError,
  GroupError,
  InternalError,
  Shift,
}



#nullable disable

internal class Parser
{
  public ParserTables         m_Tables;
  private string              m_LookaheadBuffer;
  private int                 m_CurrentLALR;
  private ParserTokenStack    m_Stack;
  private List<ParserSymbol>  m_ExpectedSymbols;
  private bool                m_HaveReduction;
  private bool                m_TrimReductions;
  private ParserTokenDeque    m_InputTokens;
  private TextReader          m_Source;
  private ParserPosition      m_SysPosition;
  private ParserPosition      m_CurrentPosition;
  private ParserTokenStack    m_GroupStack;

  public Parser(ParserTables parser_tables_)
  {
    this.m_Tables           = parser_tables_;
    this.m_Stack            = new ParserTokenStack();
    this.m_ExpectedSymbols  = new List<ParserSymbol>();
    this.m_InputTokens      = new ParserTokenDeque();
    this.m_SysPosition      = new ParserPosition();
    this.m_CurrentPosition  = new ParserPosition();
    this.m_GroupStack       = new ParserTokenStack();
    this.m_TrimReductions   = false;
    //
    this.Restart();
  }

  public void Restart()
  {
    this.m_SysPosition.Column     = 0;
    this.m_SysPosition.Line       = 0;
    this.m_CurrentPosition.Line   = 0;
    this.m_CurrentPosition.Column = 0;
    this.m_InputTokens.Clear();
    this.m_LookaheadBuffer        = "";
    this.m_GroupStack.Clear();
    this.m_CurrentLALR            = (int)this.m_Tables.LALR.InitialState;
    this.m_Stack.Clear();
    this.m_Stack.Push(new ParserToken(this.m_Tables.LALR.InitialState));
    this.m_HaveReduction          = false;
    this.m_ExpectedSymbols.Clear();
  }

  public bool TrimReductions
  {
    get => this.m_TrimReductions;
    set => this.m_TrimReductions = value;
  }


  public bool Open(TextReader Reader)
  {
    this.Restart();
    this.m_Source = Reader;
    return true;
  }


  public object CurrentReduction
  {
    get
    {
      return this.m_HaveReduction?  this.m_Stack.Top().Data : (object)null;
    }
    set
    {
      if (!this.m_HaveReduction)
        return;
      this.m_Stack.Top().Data = value;
    }
  }

  internal ParserTables Tables
  {
    get => this.m_Tables;
    set => this.m_Tables = value;
  }
  internal List<ParserSymbol> ExpectedSymbols()   => this.m_ExpectedSymbols;
  internal short CurrentLALRState()               => checked ((short) this.m_CurrentLALR);
  public ParserPosition CurrentPosition()         => this.m_CurrentPosition;
  public ParserToken CurrentToken()               => this.m_InputTokens.Top();
  public ParserToken DiscardCurrentToken()        => this.m_InputTokens.Pop();
  public void EnqueueInput(ParserToken TheToken)  => this.m_InputTokens.InsertHead(TheToken);
  public void PushInput(ParserToken TheToken)     => this.m_InputTokens.Push(TheToken);




  private Parser.ParseResult ParseLALR(ParserToken token_)
  {
    ParserLRAction      lr_action         = this.m_Tables.LALR[this.m_CurrentLALR].GetLRActionForSymbol(token_.Parent);
    Parser.ParseResult  lalr_parse_result = default;
    if (lr_action != null)
    {
      this.m_HaveReduction = false;
      switch (lr_action.Type())
      {
        case LRActionType.Shift:
          this.m_CurrentLALR  = (int) lr_action.Value();
          token_.State        = checked ((short) this.m_CurrentLALR);
          this.m_Stack.Push(token_);
          lalr_parse_result   = Parser.ParseResult.Shift;
          break;
        case LRActionType.Reduce:
          ParserProduction  production = this.m_Tables.Production[(int) lr_action.Value()];
          ParserToken       next_head_token;
          if (this.m_TrimReductions && production.ContainsOneNonTerminal())
          {
            next_head_token          = this.m_Stack.Pop();
            next_head_token.Parent   = production.HeadSymbol;
            lalr_parse_result        = Parser.ParseResult.ReduceEliminated;
          }
          else
          {
            this.m_HaveReduction = true;
            size_t                      production_handle_symbols_count     = production.HandleSymbolsCount();
            ParserReductionConstructor  reduction                           = new ParserReductionConstructor(production, production_handle_symbols_count);
            int                         production_handle_symbol_index_last = production_handle_symbols_count - 1;
            while (production_handle_symbol_index_last >= 0)
            {
              reduction.SetTokenAtIndex(production_handle_symbol_index_last, this.m_Stack.Pop());
              --production_handle_symbol_index_last;
            }
            next_head_token   = new ParserToken(production.HeadSymbol, reduction);
            lalr_parse_result = Parser.ParseResult.ReduceNormal;
          }

          short goto_lr_state_index      = this.m_Stack.Top().State;
          int   goto_lr_action_index     = this.m_Tables.LALR[goto_lr_state_index].IndexOf(production.HeadSymbol);
          if (goto_lr_action_index != -1)
          {
            this.m_CurrentLALR      = this.m_Tables.LALR[goto_lr_state_index][goto_lr_action_index].Value();
            next_head_token.State   = checked ((short) this.m_CurrentLALR);
            this.m_Stack.Push(next_head_token);
          }
          else
            lalr_parse_result = Parser.ParseResult.InternalError;

          break;
        case LRActionType.Accept:
          this.m_HaveReduction  = true;
          lalr_parse_result     = Parser.ParseResult.Accept;
          break;
      }
    }
    else
    {
      this.m_ExpectedSymbols.Clear();
      foreach (ParserLRAction expected_lr_action in this.m_Tables.LALR[this.m_CurrentLALR])
      {
        switch (expected_lr_action.Symbol.Type)
        { //TODO По видимому это должен быть отдельный метод и где-то рядом с определением SymbolType, т.к. если что-то менять, то замаешься искать где оно находится..
          case SymbolType.Content: case SymbolType.End: case SymbolType.GroupStart: case SymbolType.GroupEnd:
            this.m_ExpectedSymbols.Add(expected_lr_action.Symbol);
            break;
        }
      }
      lalr_parse_result = Parser.ParseResult.SyntaxError;
    }

    return lalr_parse_result;
  }



  private string vb_LookaheadBuffer(int count_)
  {
    //'Return Count characters from the lookahead buffer. DO NOT CONSUME
    //'This is used to create the text stored in a token. It is disgarded
    //'separately. Because of the design of the DFA algorithm, count should
    //'never exceed the buffer length. The If-Statement below is fault-tolerate
    //'programming, but not necessary.

    if (count_ > m_LookaheadBuffer.Length)
      count_ = m_LookaheadBuffer.Length;

    return m_LookaheadBuffer.Substring(0, count_);
  }

  private string vb_Lookahead(int CharIndex)
  {
    //'Return single char at the index. This function will also increase 
    //'buffer if the specified character is not present. It is used 
    //'by the DFA algorithm.

    int ReadCount = 0, n = 0;

    //'Check if we must read characters from the Stream
    if (CharIndex > m_LookaheadBuffer.Length)
    {
      ReadCount = CharIndex - m_LookaheadBuffer.Length;
      for (n = 1; n <= ReadCount; ++n)
      {
        m_LookaheadBuffer += vb_compatable_ChrW(m_Source.Read());
      }
    }

    //'If the buffer is still smaller than the index, we have reached
    //'the end of the text. In this case, return a null string - the DFA
    //'code will understand.
    if (CharIndex <= m_LookaheadBuffer.Length)
      return m_LookaheadBuffer[CharIndex - 1].ToString();
    else
      return "";
  }
  static char vb_compatable_ChrW(int CharCode)
  { // **VB**
    //return Strings.ChrW(CharCode);
    //
    //If CharCode < -32768 OrElse CharCode > 65535 Then
    //    Throw New ArgumentException(SR.Format(SR.Argument_RangeTwoBytes1, NameOf(CharCode)), NameOf(CharCode))
    //End If
    //Return Global.System.Convert.ToChar(CharCode And &HFFFFI)

    if (CharCode < -32768 || CharCode > 65535)
      throw new ArgumentException("CharCode < -32768 || CharCode > 65535");

    return System.Convert.ToChar(CharCode & 0xFFFF);    
  }
  static int vb_compatable_AscW(string str)
  { // **VB**
    //return Strings.AscW(str);
    return str[0];
  }



  private ParserToken v5x_LookaheadDFA()
  {
    //'This function implements the DFA for th parser's lexer.
    //'It generates a token which is used by the LALR goto_lr_state_index
    //'machine.

    string  Ch = "";
    int     n = 0, /*TargetFAStateIndex,*/ CurrentDFA = 0;
    int     Target = 0;
    bool    Found = false, Done = false;
    ParserFAEdge  Edge = null;
    int     CurrentPosition = 0;
    int     LastAcceptState = 0, LastAcceptPosition = 0;    
    //TODO  При такой реализации метода компилятор не чувствует, что он когда-то закончится и поэтому не дает делать неприсвоенные значения
    //      рефакторить до простоты и прозрачности надо
    string  result_token_data = null;
    ParserSymbol  result_token_symb = null;
    //'===================================================
    //'Match DFA token
    //'===================================================

    Done = false;
    CurrentDFA = m_Tables.DFA.InitialState;
    CurrentPosition = 1;               //'Next byte in the input Stream
    LastAcceptState = -1;              //'We have not yet accepted a character string
    LastAcceptPosition = -1;

    Ch = vb_Lookahead(1);

    if( !(Ch == "" || vb_compatable_AscW(Ch) == 65535) ) //'NO MORE DATA
    {
      while(!Done)
      {
      //' This code searches all the branches of the current DFA goto_lr_state_index
      //' for the next character in the input Stream. If found the
      //' target goto_lr_state_index is returned.

        Ch = vb_Lookahead(CurrentPosition);
        if (Ch == "")     //'End reached, do not match
            Found = false;
        else
        { 
            n = 0;
            Found = false;
            while(n < m_Tables.DFA[CurrentDFA].EdgeList.Count() && !Found)
            {
                Edge = m_Tables.DFA[CurrentDFA].EdgeList[n];

                //'==== Look for character in the Character Set Table
                if (Edge./*Characters.*/Contains(vb_compatable_AscW(Ch))) 
                {
                    Found = true;
                    Target = Edge.Target; //'.TableIndex
                }                        
                n += 1;
            }
        }

        //' This block-if statement checks whether an edge was found from the current goto_lr_state_index. If so, the goto_lr_state_index and current
        //' position advance. Otherwise it is time to exit the main loop and report the token found (if there was one). 
        //' If the LastAcceptState is -1, then we never found a match and the Error Token is created. Otherwise, a new 
        //' token is created using the Symbol in the Accept State and all the characters that comprise it.

        if (Found)// Then
        {
          //' This code checks whether the target goto_lr_state_index accepts a token.
          //' If so, it sets the appropiate variables so when the
          //' algorithm in done, it can return the proper token and
          //' number of characters.

          if(m_Tables.DFA[Target].AcceptSymbol != null) //'NOT is very important!
          {
              LastAcceptState     = Target;
              LastAcceptPosition  = CurrentPosition;
          }

          CurrentDFA      = Target;
          CurrentPosition += 1;
        }
        else //'No edge found
        {
          Done = true;
          if (LastAcceptState == -1)      //' Lexer cannot recognize symbol
          {
              result_token_symb = m_Tables.Symbol.GetSymbol_Err();// GetFirstOfType(SymbolType.Error);
              result_token_data = vb_LookaheadBuffer(1);
          }
          else                            //' Construct Token, read characters
          {
              result_token_symb = m_Tables.DFA[LastAcceptState].AcceptSymbol;
              result_token_data = vb_LookaheadBuffer(LastAcceptPosition);   //'Data contains the total number of accept characters
          }          
        }
      }
    }
    else
    { 
        //' End of file reached, create End Token
        result_token_data = "";
      result_token_symb = m_Tables.Symbol.GetSymbol_End();// GetFirstOfType(SymbolType.End);
    }

    //'===================================================
    //'Set the new token's position information
    //'===================================================
    //'Notice, this is a copy, not a linking of an instance. We don't want the user 
    //'to be able to alter the main value indirectly.
    Debug.Assert(result_token_symb != null && result_token_data != null);
    return new ParserToken(result_token_symb, result_token_data, m_SysPosition);
  }


  private void ConsumeBuffer(int CharCount)
  {
    if (CharCount > this.m_LookaheadBuffer.Length)
      return;
    int num = checked (CharCount - 1);
    int index = 0;
    while (index <= num)
    {
      switch ((char) ((int) this.m_LookaheadBuffer[index] - 10))
      {
        case char.MinValue:
          checked { ++this.m_SysPosition.Line; }
          this.m_SysPosition.Column = 0;
          goto case '\u0003';
        case '\u0003':
          checked { ++index; }
          continue;
        default:
          checked { ++this.m_SysPosition.Column; }
          goto case '\u0003';
      }
    }
    this.m_LookaheadBuffer = this.m_LookaheadBuffer.Remove(0, CharCount);
  }

  
  void concatenate_tokens_data(ParserToken a_target_, ParserToken b_add_)
  {
    Debug.Assert(a_target_.Data is string);
    Debug.Assert(b_add_.Data is string);
    a_target_.Data = (string)a_target_.Data + (string)b_add_.Data;
  }
  void concatenate_tokens_data_1_char(ParserToken a_target_, ParserToken b_add_)
  {
    Debug.Assert(a_target_.Data is string);
    Debug.Assert(b_add_.Data is string);
    a_target_.Data = (string)a_target_.Data + ((string)b_add_.Data)[0];
  }

  private ParserToken ProduceToken()
  {
    bool b_done = false;
    ParserToken result_token = (ParserToken) null;

    while (!b_done)
    {
      ParserToken lookahead_token = this.v5x_LookaheadDFA();
      // _LookaheadDFA() всегда возвращает нам токен у которого .Data уставновлена в строку, которую DFA прочитал из входного потока
      //  (или пустую строку, если ничего не прочитал). НО ВСЕГДА .Data уставновлена в строку !!!
      Debug.Assert(lookahead_token.Data is string);
      int lookahead_token_data_len = ((string)lookahead_token.Data).Length;

      if (lookahead_token.Type() == SymbolType.GroupStart && (this.m_GroupStack.Count == 0 || this.m_GroupStack.Top().Group().NestingContains((int) lookahead_token.Group().TableIndex)))
      {        
        this.ConsumeBuffer(lookahead_token_data_len);
        this.m_GroupStack.Push(lookahead_token);
      }
      else if (this.m_GroupStack.Count == 0)
      {        
        this.ConsumeBuffer(lookahead_token_data_len);
        result_token = lookahead_token;
        b_done = true;
      }
      else if (this.m_GroupStack.Top().Group().End == lookahead_token.Parent)
      {
        ParserToken group_stack_pop_token = this.m_GroupStack.Pop();
        if (group_stack_pop_token.Group().Ending == GroupEndingMode.Closed)
        {
          concatenate_tokens_data(group_stack_pop_token, lookahead_token);
          this.ConsumeBuffer(lookahead_token_data_len);
        }
        if (this.m_GroupStack.Count == 0)
        {
          group_stack_pop_token.Parent = group_stack_pop_token.Group().Container;
          result_token = group_stack_pop_token;
          b_done = true;
        }
        else
          concatenate_tokens_data(this.m_GroupStack.Top(), group_stack_pop_token);          
      }
      else if (lookahead_token.Type() == SymbolType.End)
      {
        result_token = lookahead_token;
        b_done = true;
      }
      else
      {
        ParserToken group_stack_top_token = this.m_GroupStack.Top();
        if (group_stack_top_token.Group().Advance == GroupAdvanceMode.Token)
        { 
          concatenate_tokens_data(group_stack_top_token, lookahead_token);
          this.ConsumeBuffer(lookahead_token_data_len);
        }
        else
        {
          concatenate_tokens_data_1_char(group_stack_top_token, lookahead_token);
          this.ConsumeBuffer(1);
        }
      }
    }

    return result_token;
  }


  public ParseMessage Parse()
  {
    if (!this.m_Tables.IsLoaded())
      return ParseMessage.NotLoadedError;

    bool          b_done        = false;
    //TODO  Надо переделать как в плюсах.
    //      Возвращать по-месту и учесть что при m_InputTokens.Pop() может быть неожиданная ошибка пустого стека.
    ParseMessage parse_result  = 0;
    while (!b_done)
    {
      if (this.m_InputTokens.Empty() )
      {
        this.m_InputTokens.Push(this.ProduceToken());
        parse_result = ParseMessage.TokenRead;
        b_done = true;
      }
      else
      {
        ParserToken next_token = this.m_InputTokens.Top();
        this.m_CurrentPosition.Assign(next_token.Position);
        if (this.m_GroupStack.Count != 0)
        {
          parse_result = ParseMessage.GroupError;
          b_done = true;
        }
        else if (next_token.Type() == SymbolType.Noise)
          this.m_InputTokens.Pop();
        else if (next_token.Type() == SymbolType.Error)
        {
          parse_result = ParseMessage.LexicalError;
          b_done = true;
        }
        else
        {
          switch (this.ParseLALR(next_token))
          {
            case Parser.ParseResult.Accept:
              parse_result = ParseMessage.Accept;
              b_done = true;
              break;
            case Parser.ParseResult.Shift:
              this.m_InputTokens.Pop();
              parse_result = ParseMessage.Shift;
              b_done = true;
              break;
            case Parser.ParseResult.ReduceNormal:
              parse_result = ParseMessage.Reduction;
              b_done = true;
              break;
            case Parser.ParseResult.SyntaxError:
              parse_result = ParseMessage.SyntaxError;
              b_done = true;
              break;
            case Parser.ParseResult.InternalError:
              parse_result = ParseMessage.InternalError;
              b_done = true;
              break;
          }
        }
      }
    }
    return parse_result;
  }

  private enum ParseResult
  {
    Accept = 1,
    Shift = 2,
    ReduceNormal = 3,
    ReduceEliminated = 4,
    SyntaxError = 5,
    InternalError = 6,
  }
}
