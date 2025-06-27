//
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
//
namespace gpp.builder;


internal static partial class GrammarParseHelper
{ 
  internal static string TokenText(object token_data_object_)
  {
    return TokenText(token_data_object_.ToString());
  }
  internal static string TokenText(string token_text_)
  {
    //TODO  Оригинальный алгоритм "вынимал" содержимое из "заковыченных" строк просто .Substring(1, s.Length - 2)
    //      не учитывая какая там последняя кавычка была (или небыла)
    //      нужно бы доработать.
    //      И возможно после убирания кавычек нужно делать Trim
    static string quoted_text_body(string s)
    { 
      return s.Substring(1, s.Length - 2);
    }
    if (token_text_.Length >= 2)
    {
      string token_text_result;
      switch (token_text_[0])
      {
        case '"':
          {
            token_text_result = quoted_text_body(token_text_);
            break;
          }
        case '\'':
          {
            string token_text_body = quoted_text_body(token_text_);
            //TODO  тут как-то малопонятно что и зачем.. Если было две кавычки сделали из них одну? 
            token_text_result = token_text_.Equals("''")? "'" : GrammarParseHelper.RemoveMultiSpaces(token_text_body);
            // в оригинале было так:
            //token_text_result = Operators.CompareString(may_be_number_string_, "''", true) != 0 ? SelfGrammar.RemoveMultiSpaces(token_text_body) : "'";
            break;
          }
        case '<':
          {
            string token_text_body = quoted_text_body(token_text_);
            token_text_result = GrammarParseHelper.RemoveMultiSpaces(token_text_body);
            break;
          }
        case '[':
          {            
            string token_text_body = quoted_text_body(token_text_);
            token_text_result = GrammarParseHelper.SetLiteral(token_text_body);
            break;
          }
        default:
          token_text_result = token_text_;
          break;
      }
      return token_text_result;
    }
    else
      return token_text_;
  }

  private static string SetLiteral(string source_string_)
  {
    string  text_in_single_quotes   = "";
    bool    b_state_in_single_quote = false;
    string  result_literal_string   = "";
    for (int source_char_index = 0; source_char_index < source_string_.Length; ++source_char_index)
    {
      //TODO  Реально на char переделать надо
      string source_char = source_string_[source_char_index].ToString();
      if (b_state_in_single_quote)
      {
        if (source_char.Equals("'"))
        {
          source_char = (text_in_single_quotes.Equals("")) ? "'" : text_in_single_quotes;
          b_state_in_single_quote = false;
        }
        else
        {
          text_in_single_quotes += source_char;
          source_char = "";
        }
      }
      else if (source_char.Equals("'"))
      {
        b_state_in_single_quote = true;
        text_in_single_quotes = "";
        source_char = "";
      }
      result_literal_string += source_char;
    }
    return result_literal_string;
  }


  private static string RemoveMultiSpaces(string text_)
  {
    //TODO  Метод удаляет множественные пробелы из строки, но, кажется, алгоритм можно улучшить без выделения памяти, хз.. как в шарпе..
    string result_text = "";
    bool b_prev_was_non_space = false;

    for (int i = 0; i < text_.Length; ++i)
    {
      char ch = text_[i];
      if (ch == ' ')
      {
        if (b_prev_was_non_space)
          result_text += " ";
        b_prev_was_non_space = false;
      }
      else
      {
        result_text += ch.ToString();
        b_prev_was_non_space = true;
      }
    }
    return result_text;
  }


  internal static bool GetCharcodeFromDec(string may_be_number_string_, out int out_charcode_)
  {
    //TODO  Такое конвертирование в число может иногда отличаться от того, как делал бейсик Val
    //      поэтому оригинальный алгоритм пока оставляю заремленным
    //      чтобы он работал (или посмотреть алгоритм) нужно подключить using Microsoft.Visua_Basic.CompilerServices;
    //TODO  Также все равно алгоритм странный
    //      зачем .Substring(1) ? - ДЕСЯТИЧНОЕ представление с префиксом # - #1234 - проверять его в общем-то ненужно, т.к. парсер все проверяет. разве что Debug.Assert
    //      - почему только ushort ? возможно этот метод используется только для конвертирования числового десятичного представления символа (char) ?
    //      ДА: используется в ProductionIndex.Charcodevalue_Decnumber
    //      И часть обработки ошибки (если -1) - там
    //      НУЖНО переделывать и здесь и там, чтобы понятно было что к чему
    string s = may_be_number_string_.Substring(1);
    bool b = int.TryParse(s, out int num_value);
    if (b && (num_value >= 0 && num_value <= ushort.MaxValue))
    {
      out_charcode_ = num_value;
      return true;
    }
    out_charcode_ = 0;
    return false;
  }
  internal static bool GetCharcodeFromHex(string may_be_number_string_, out int out_charcode_)
  {
    string s = may_be_number_string_.Substring(1);
    bool b = int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int num_value);
    if (b && (num_value >= 0 && num_value <= ushort.MaxValue))
    {
      out_charcode_ = num_value;
      return true;
    }
    out_charcode_ = 0;
    return false;
  }


  internal static void WarnRegexSetLiteral(AppLog log_, string FullText, int CurrentLineNumber)
  {
    //TODO  Здесь VB-оператор Like всего лишь проверяет, чтобы ProductionIndex.Setitem_Setliteral 
    //      была в правильной форме потому что согласно документации http://www.goldparser.org/doc/grammars/source_char_index.htm
    //      "Set ranges can be specified by using a ".." between two values. Both the start and end values can be in either decimal or hexadecimal."
    //      то есть Example3 = {#65 .. #70}	=> ABCDEF =>	This set range defines a set from from the letter 'A' (#65) to 'F' (#70).
    //      Но видимо многие ошибаются и пишут диапазон как [A-Z], что Голд понимает как "three characters: 'A', '-', and 'Z'"
    //      Поэтому если TokenText "похож" на "*[a-zA-Z]-[a-zA-Z]*" или "*#-#*" - т.е. 'буква''тире''буква' или 'число''тире''число'
    //        (для VB-Like символ # означает "any single digit (0 - 9)" - гм... как-то все равно не совсем понятно...)
    //      ТО дается AppLogAlert.Warning на всякий случай
    //TODO  Как-то, конечно странно... Не должен парсер неоднозначностей допускать - эти ситуации он должен разрулить сам - или понятно или ошибка
    //      вроде как запись вида [A-Z] должна попасть вообще в другую продукцию типа ProductionIndex.Setitem_Lbrace_Rbrace
    //      Возможно это артефакт первоначальной версии грамматики и это вообще убрать можно, хз..
    //      Проверять его грамматику!
    //bool b_chr = Regex.IsMatch(" aB -X", @"\s*[a-zA-Z]\s*-\s*[a-zA-Z]\s*");   - 'может-пробелы''БуКВы''тире''может-пробелы''БуКВы''может-пробелы'
    //bool b_num = Regex.IsMatch(" 18 -988 ", @".\s*\d+\s*-\s*\d+\s*");         - 'может-пробелы''цифры-одна-или-больше''тире''может-пробелы''цифры-одна-или-больше''может-пробелы'    
    string token_text = GrammarParseHelper.TokenText(FullText);
    if (MyRegexLike_CharsRangeLetters().IsMatch(token_text) || MyRegexLike_CharsRangeDigits().IsMatch(token_text))
    {
      log_.Add(AppLogSection.Grammar, AppLogAlert.Warning, "The set literal " + FullText + " does not represent a set range",
          "The GOLD Builder interpreters sets like [A-Z] as three characters: 'A', '-', and 'Z'. Set ranges are supported, but use a different notation. Please consult the documentation.", CurrentLineNumber.ToString());
    }
  }


  // приватные компилируемые регекспы
  [GeneratedRegex(@".\s*\d+\s*-\s*\d+\s*")]
  private static partial Regex MyRegexLike_CharsRangeDigits();

  [GeneratedRegex(@"\s*[a-zA-Z]\s*-\s*[a-zA-Z]\s*")]
  private static partial Regex MyRegexLike_CharsRangeLetters();

}

