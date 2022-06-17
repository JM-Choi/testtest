using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#pragma warning disable CS0162

namespace tech_library.Tools
{
    public class UtilMgrCustomException : Exception
    {
        public UtilMgrCustomException() : base()
        {
        }

        public UtilMgrCustomException(string msg) : base(msg)
        {
        }
    }

    public static class UtilMgr
    {
        /// <summary>
        /// unixtimestamp ==> datetime 으로 변환
        /// </summary>
        /// <param name="unixTimeStamp">unixTimeStamp</param>
        /// <returns>datetime</returns>
        public static DateTime UnixtimeStamptoDateTime(double unixTimeStamp)
        {
            System.DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dt = dt.AddSeconds(unixTimeStamp).ToLocalTime();
            return dt;
        }

        /// <summary>
        /// datetime string ==> formatted datetime 으로 변환
        /// </summary>
        /// <param name="strtime">string typed datetime</param>
        /// <param name="formatted">format = "yyyy-MM-dd HH:mm:ss.fff"</param>
        /// <returns>datetime</returns>
        public static DateTime StringToDatetimeInFormated(string strtime, string formatted)
        {
            return DateTime.ParseExact(strtime, formatted, null);
        }

        /// <summary>
        /// 이미 split 된 string [] 에서 key 를 포함하는 string 을 서치
        /// </summary>
        /// <param name="words">RV 에서 수신한 문자열을 공백으로 구분한 string 배열</param>
        /// <param name="key">words 에서 찾을 키 문자열</param>
        /// <returns>key 를 포함하는 List형 문자열 or null</returns>
        public static List<string> FindString(string[] words, string key)
        {
            var items = from a in words
                        where a.ToUpperInvariant().Contains(key)
                        select a;
            if (items.Count() > 0)
                return items.ToList();
            return null;
        }

        /// <summary>
        /// 이미 split 된 string [] 에서 key 를 포함하는 string 을 서치, SUBEQPID 에는 EQPID 가 포함
        /// 중복검색건에 대해 정확하게 key 를 다시 한 번 비교
        /// </summary>
        /// <param name="words"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static List<string> FindStringDistinct(string[] words, string key)
        {
            int err = 0;
            List<string> lst = FindString(words, key);
            if (lst == null)
            {
                err = 1;
                goto err_FindStringDistinct;
            }
            foreach (var s in lst)
            {
                string[] t = s.Split('=');
                if (t.Count() < 2)
                {
                    err = 2;
                    goto err_FindStringDistinct;
                }
                if (t[0] == key)
                {
                    return (new List<string>() { s });
                }
            }

            err = 3;

        err_FindStringDistinct:
            switch (err)
            {
                case 1: throw new Exception("Not found string."); break;
                case 2: throw new Exception("Not found data."); break;
                case 3: throw new Exception("Not found key string."); break;
            }
            return null;
        }

        /// <summary>
        /// 이미 split 된 string [] 에서 key 에 해당하는 value 검색. 유일한지 내부 비교수행
        /// </summary>
        /// <param name="words"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string FindKeyStringToValue(string[] words, string key)
        {
            try
            {
                int err = 0;
                List<string> lst = FindString(words, key);
                if (lst == null)
                {
                    err = 1;
                    goto err_FindKeyStringToValue;
                }
                string[] t = lst[0].ToString().Split('=');
                if (t.Count() < 2)
                {
                    err = 2;
                    goto err_FindKeyStringToValue;
                }
                if (t[0] == key)
                {
                    return t[1];
                }
                err = 3;

            err_FindKeyStringToValue:
                switch (err)
                {
#pragma warning disable CS0436 // 형식이 가져온 형식과 충돌합니다.
                    case 1: throw new UtilMgrCustomException("Not found string."); break;
                    case 2: throw new UtilMgrCustomException("Not found data."); break;
                    case 3: throw new UtilMgrCustomException("Not found key string."); break;
#pragma warning restore CS0436 // 형식이 가져온 형식과 충돌합니다.
                }
            }
            catch (Exception ex)
            {
            }
            return "Not found";
        }

        /// <summary>
        /// 문자열내에 \t 을 삭제한다. \t 은 문자열내에서 2개이상 whitespace 의 연속으로 표시된다.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string ClearTabChar(string msg)
        {
            msg = msg.Replace("\r\n", string.Empty);
            const string reduceMultiSpace = @"[ ]{2,}";
            msg = Regex.Replace(msg.Replace("\t", " "), reduceMultiSpace, " ");
            return msg;
        }
    }
}
