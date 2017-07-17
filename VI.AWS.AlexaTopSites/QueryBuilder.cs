using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp.Extensions.MonoHttp;

namespace VI.AWS.AlexaTopSites
{
    internal class QueryBuilder : IEnumerable
	{
		private readonly RestRequest _request;

		private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();

		public QueryBuilder(RestRequest request)
		{
			_request = request;
		}

		public void Add(string name, object value)
		{
			if (value != null)
			{
				_request.AddParameter(name, value);
				_parameters.Add(name, value);
			}
		}

		public string Query
		{
			get 
			{
				var queryString = new StringBuilder();
				var sorted = _parameters.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray();
				foreach (var v in sorted)
				{
					if (queryString.Length > 0)
						queryString.Append("&");

					queryString.AppendFormat("{0}={1}", v.Key, v.Value.ToString().UpperCaseUrlEncode());
				}
				return queryString.ToString(); 
			}
		}

	    public IEnumerator GetEnumerator()
	    {
	        return _parameters.GetEnumerator();
	    }
	}

	internal static class StringHelper
	{
		public static string UpperCaseUrlEncode(this string s)
		{
			
			char[] temp = HttpUtility.UrlEncode(s).ToCharArray();
			for (int i = 0; i < temp.Length - 2; i++)
			{
				if (temp[i] == '%')
				{
					temp[i + 1] = char.ToUpper(temp[i + 1]);
					temp[i + 2] = char.ToUpper(temp[i + 2]);
				}
			}
			return new string(temp);
		}
	}
}
