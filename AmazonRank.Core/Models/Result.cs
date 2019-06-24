using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonRank.Core
{
    public class Result<T>
    {
        public Result() : this(false, string.Empty) { }

        public Result(bool success, string msg)
        {
            this.Success = success;
            this.Msg = msg;
        }

        public Result(bool success, string msg, T data)
        {
            this.Success = success;
            this.Msg = msg;
            this.Data = data;
        }

        public bool Success { get; set; }

        public string Msg { get; set; }

        public T Data { get; set; }

        /// <summary>
        /// 成功构造函数
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result<T> OK(string msg, T data)
        {
            return new Result<T>(true, msg, data);
        }

        /// <summary>
        /// 成功构造函数
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result<T> OK(T data)
        {
            return new Result<T>(true, "", data);
        }

        /// <summary>
        /// 成功构造函数
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result<T> OK(string msg = "")
        {
            return OK(msg, default(T));
        }

        /// <summary>
        /// 失败构造函数
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result<T> Error(string msg)
        {
            return new Result<T>(false, msg);
        }
    }
}
