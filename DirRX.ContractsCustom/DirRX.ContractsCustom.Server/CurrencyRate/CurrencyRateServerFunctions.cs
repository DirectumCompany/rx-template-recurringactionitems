using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.CurrencyRate;
using System.Net;
using System.Text;
using System.Xml;
using System.IO;

namespace DirRX.ContractsCustom.Server
{
  partial class CurrencyRateFunctions
  {
    #region Работа с курсами
    /// <summary>
    /// Вернуть результат выполнения GET запроса.
    /// </summary>
    /// <param name="url">Url запроса.</param>
    /// <returns>Результат выполнения.</returns>
    private static string GetRequestResult(string url)
    {
      var request = HttpWebRequest.Create(url);
      var resp = (HttpWebResponse)request.GetResponse();
      
      string result;
      using (var stream = resp.GetResponseStream())
        using (var reader = new StreamReader(stream, Encoding.GetEncoding("windows-1251")))
          result = reader.ReadToEnd();
      
      return result;
    }
    
    /// <summary>
    /// Вернуть информацию по валютам, которые есть в справочнике "Валюты".
    /// </summary>
    /// <returns>Перечень данных по валютам: запись справочника и код с сайта ЦБ РФ.</returns>
    public static System.Collections.Generic.IEnumerable<Structures.Module.ICurrencyForQuery> GetCurrencies()
    {
      string currData = null;
      try
      {
        currData = GetRequestResult("http://www.cbr.ru/scripts/XML_valFull.asp?d=0");
        Logger.Debug("Currencies download complete.");
      }
      catch (WebException e)
      {
        Logger.ErrorFormat("Currencies download error: {0}", e.Message);
      }
      var doc = new XmlDocument();
      doc.LoadXml(currData);
      
      var currencyNodes = doc.SelectNodes("/Valuta/Item");
      
      // Добавить в результирующий список только валюты, которые есть в справочнике.
      var result = new List<Structures.Module.ICurrencyForQuery>();
      
      foreach (var curr in Functions.CurrencyRate.GetAllCurrencies())
      {
        var numCode = curr.NumericCode;
        foreach (XmlNode node in currencyNodes)
        {
          if (node["ISO_Num_Code"].InnerText == numCode)
          {
            var newItem = Structures.Module.CurrencyForQuery.Create();
            newItem.Currency = curr;
            newItem.Code = node.Attributes["ID"].Value;
            result.Add(newItem);
            break;
          }
        }
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить все курсы указанной валюты, начиная с конкретной даты.
    /// </summary>
    /// <param name="currencyInfo">Информация о валюте.</param>
    /// <param name="fromDate">Дата, начиная с которой нужно получить курсы.</param>
    /// <returns>Перечень курсов валюты.</returns>
    public static System.Collections.Generic.IEnumerable<Structures.Module.IRateDate> GetRatesForCurrency(Structures.Module.ICurrencyForQuery currencyInfo, DateTime fromDate)
    {
      string currData = null;
      try
      {
        var url = String.Format("http://www.cbr.ru/scripts/XML_dynamic.asp?date_req1={0:dd/MM/yyyy}&date_req2={1:dd/MM/yyyy}&VAL_NM_RQ={2}", fromDate, Calendar.Today, currencyInfo.Code);
        currData = GetRequestResult(url);
        Logger.DebugFormat("Rates for currency {0} download complete.", currencyInfo.Currency);
      }
      catch (WebException e)
      {
        Logger.ErrorFormat("Rates for currency {0} download error: {1}", currencyInfo.Currency, e.Message);
      }
      
      var separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
      var result = new List<Structures.Module.IRateDate>();
      
      var doc = new XmlDocument();
      doc.LoadXml(currData);
      
      var currencyNodes = doc.SelectNodes("/ValCurs/Record");
      
      foreach (XmlNode node in currencyNodes)
      {
        var nominal = Int32.Parse(node["Nominal"].InnerText);
        
        var rateValue = Double.Parse(node["Value"].InnerText.Replace(",", separator));
        
        var newItem = Structures.Module.RateDate.Create();
        newItem.Date = DateTime.ParseExact(node.Attributes["Date"].InnerText, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
        newItem.Rate = rateValue / nominal;
        result.Add(newItem);
      }
      
      return result;
    }
    
    /// <summary>
    /// Создать курсы валюты.
    /// </summary>
    /// <param name="currency">Валюта.</param>
    /// <param name="rates">Перечень курсов в разрезе дат.</param>
    public static void CreateRates(Sungero.Commons.ICurrency currency, System.Collections.Generic.IEnumerable<Structures.Module.IRateDate> rates)
    {
      foreach (var rate in rates)
      {
        var newRate = CurrencyRates.Create();
        newRate.Date = rate.Date;
        newRate.Currency = currency;
        newRate.Rate = rate.Rate;
        newRate.Name = DirRX.ContractsCustom.CurrencyRates.Resources.CurrencyRateFormatNameFormat(currency.Name, rate.Date.ToShortDateString());
        try
        {
          newRate.Save();
        }
        catch (Exception ex)
        {
          Logger.Error(ex.Message);
        }
      }
    }
    
    /// <summary>
    /// Вернуть все валюты из справочника.
    /// </summary>
    /// <returns>Перечень валют.</returns>
    [Public, Remote]
    public static IQueryable<Sungero.Commons.ICurrency> GetAllCurrencies()
    {
      return Sungero.Commons.Currencies.GetAll();
    }
    
    /// <summary>
    /// Получить последнюю дату курса указанной валюты.
    /// </summary>
    /// <param name="currency">Валюта.</param>
    /// <returns>Последняя дата курса.</returns>
    [Remote]
    public static DateTime? GetLastRateDate(Sungero.Commons.ICurrency currency)
    {
      return CurrencyRates.GetAll(r => Sungero.Commons.Currencies.Equals(r.Currency, currency)).Max(r => r.Date);
    }
    
    
    /// <summary>
    /// Получить запись справочника "Курс валюты" на конкретную дату.
    /// </summary>
    /// <param name="currency">Валюта.</param>
    /// <param name="date">Дата.</param>
    /// <returns>Запись "Курс валюты".</returns>
    [Remote, Public]
    public static ICurrencyRate GetRatesForCurrencyOnDate(Sungero.Commons.ICurrency currency, DateTime date)
    {
      return CurrencyRates.GetAll(cr => Sungero.Commons.Currencies.Equals(cr.Currency, currency) && cr.Date == date).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить запись справочника "Курс валюты" на конкретную дату, либо последнюю созданную.
    /// </summary>
    /// <param name="currency">Валюта.</param>
    /// <param name="date">Дата.</param>
    /// <returns>Запись "Курс валюты".</returns>
    [Remote, Public]
    public static ICurrencyRate GetRatesForCurrencyOnDateOrLast(Sungero.Commons.ICurrency currency, DateTime date)
    {
      return CurrencyRates.GetAll(cr => Sungero.Commons.Currencies.Equals(cr.Currency, currency) && cr.Date <= date).OrderByDescending(r => r.Date).FirstOrDefault();
    }
    
    /// <summary>
    /// Вернуть курс на указанную дату. Если на указанную дату нет курса, то вернуть курс на ближайшую предыдущую.
    /// Если до указанной даты курсов не было, вернуть ближайший из курсов на следующие дни.
    /// Если занесенных в систему курсов нет, то вернуть null.
    /// </summary>
    /// <param name="currency">Валюта.</param>
    /// <param name="date">Дата.</param>
    /// <returns>Курс.</returns>
    [Remote, Public]
    public static double? GetCurrencyRateForDate(Sungero.Commons.ICurrency currency, DateTime date)
    {
      var rateForDate = CurrencyRates.GetAll(r => Sungero.Commons.Currencies.Equals(r.Currency, currency) && r.Date <= date).OrderByDescending(r => r.Date).FirstOrDefault();
      if (rateForDate == null)
      {
        rateForDate = CurrencyRates.GetAll(r => Sungero.Commons.Currencies.Equals(r.Currency, currency) && r.Date > date).OrderBy(r => r.Date).FirstOrDefault();
        if (rateForDate == null)
          return null;
      }
      
      return rateForDate.Rate;
    }
    
    /// <summary>
    /// Получить валюту "Российский рубль".
    /// </summary>
    [Remote, Public]
    public static Sungero.Commons.ICurrency GetCurrencyRUB()
    {
      var rub = Sungero.Commons.Currencies.GetAll(cr => cr.AlphaCode == Constants.CurrencyRate.RubAlphaCode).FirstOrDefault();
      if (rub == null)
        Logger.Debug("No currency RUB in Currencies databook.");
      return rub;
    }
    
    /// <summary>
    /// Получить валюту "Доллар США".
    /// </summary>
    [Remote, Public]
    public static Sungero.Commons.ICurrency GetCurrencyUSD()
    {
      var usd = Sungero.Commons.Currencies.GetAll(cr => cr.AlphaCode == Constants.CurrencyRate.UsdAlphaCode).FirstOrDefault();
      if (usd == null)
        Logger.Debug("No currency USD in Currencies databook.");
      return usd;
    }
    
    /// <summary>
    /// Сконвертировать в рубли сумму по курсу на текущий момент.
    /// </summary>
    /// <param name="summ">Сумма.</param>
    /// <param name="currency">Валюта.</param>
    /// <returns>Сумма в рублях.</returns>
    [Remote(IsPure = true), Public]
    public static double GetSummInRUB(double summ, Sungero.Commons.ICurrency currency)
    {
      if (currency == GetCurrencyRUB())
        return summ;
      
      double amount = summ;
      var rateNow = GetRatesForCurrencyOnDateOrLast(currency, Calendar.Now);
      if (rateNow != null && rateNow.Rate.HasValue)
        amount = rateNow.Rate.Value * summ;
      return amount;
    }
    
    /// <summary>
    /// Сконвертировать в доллары сумму по курсу на текущий момент.
    /// </summary>
    /// <param name="summ">Сумма.</param>
    /// <param name="currency">Валюта.</param>
    /// <returns>Сумма в долларах.</returns>
    [Remote(IsPure = true), Public]
    public static double GetSummInUSD(double summ, Sungero.Commons.ICurrency currency)
    {
      if (currency == GetCurrencyUSD())
        return summ;
      
      // Сначала переведем в рубли.
      double  amount = GetSummInRUB(summ, currency);
      // Потом сконвертируем рубли в доллары.
      var currencyUSD = GetCurrencyUSD();
      var rateNow = GetRatesForCurrencyOnDateOrLast(currencyUSD, Calendar.Now);
      if (rateNow != null && rateNow.Rate.HasValue)
        amount = amount / rateNow.Rate.Value;
      return amount;
    }

    #endregion

  }
}