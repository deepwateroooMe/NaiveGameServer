using MyServer.logic;
using MySql.Data.MySqlClient;
using Nancy.Json;
using System.Text.RegularExpressions;
namespace MyServer.db {
    public class DbManager {

        public static MySqlConnection mysql;
        static JavaScriptSerializer Js;

        // 连接数据库
        public static bool Connect(string db, string ip, int port, string user,string pw) {
            mysql = new MySqlConnection();
            Js = new JavaScriptSerializer();
            // 连接参数
            string s = string.Format(
                "Database={0};" +
                "Data Source ={1};" +
                "port={2};" +
                "User Id={3};" +
                "Password={4}",
                db, ip, port, user, pw);
            // 连接参数: 这里连接不成功
            // string s = string.Format(
            //     "Database={0};" + // Database
            //     // "Initial Catalog={0};" +
            //     // "Data Source={1};" +
            //     "Database={1};" + // Data Source
            //     // "IntegratedSecurity=yes;" +
            //     // "Uid={2}", 
            //     // "Integrated Security=True",
            //     // "port={2};" +
            //     "Uid={2};" + // Uid
            //     "Pwd={3};" + // Password
            //     "OldGuids=True", 
            //     db, ip, user, pw);
            //     // db, ip, user);
            Console.WriteLine(s);
            mysql.ConnectionString = s; 
            // 连接
            try {
                mysql.Open();
                Console.WriteLine("[数据库] 打开成功");
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("[数据库] 打开失败" + " " + e.Message);
                return false;
            }
        }

        // 判断安全字符
        private static bool IsSafeString(string str) {
            return !Regex.IsMatch(str, @"[-|;|,|\/|\(|\)|\[|\]|\}|\{|%|@|\*|!|\']");
        }

        // 注册账号
        // 判断表里是否已经有这个账号了
        public static bool IsAccountExist(string id) {
            if (IsSafeString(id)) {
                return false;
            }
            // SQL语句
            string s = string.Format("select * from account where id ='{0}';", id);
            Console.WriteLine(s);// 测试语句
            try {
                MySqlCommand cmd = new MySqlCommand(s, mysql);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool hasRows = dataReader.HasRows;
                dataReader.Close();
                return !hasRows;
            }
            catch (Exception e ) {
                Console.WriteLine("[数据库] IsSafeString err " + e.Message);
                return false;
            }
        }

// 注册
        public static bool Register(string id, string pw) {
            // 安全检查
            if (!DbManager.IsSafeString(id)) {
                Console.WriteLine("[数据库]用户注册失败,安全检查未通过");
                return false;
            }
            if (!DbManager.IsSafeString(pw)) {
                Console.WriteLine("[数据库]用户注册失败,安全检查未通过");
                return false;
            }
            // 是否可以注册
            if (DbManager.IsAccountExist(id)) {
                Console.WriteLine("[数据库]用户注册失败,已经有此用户名");
                return false;
            }
            // 写入数据库
            string sql = string.Format("insert into account set id ='{0}', pw ='{1}';", id, pw);
            // string sql = string.Format("inset into account ('id','pw')values ('{0}','{1}');", id ,pw );
            Console.WriteLine("注册语句:" + sql);
            try {
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e ) {
                Console.WriteLine("[数据库]account写入数据库失败"+e .Message );
                return false; 
            }
        }

        // 创建角色
        public static bool CreatPlayer(string id) {
            // 安全检查
            if (!DbManager.IsSafeString(id)) {
                Console.WriteLine("[数据库]用户注册失败,安全检查未通过");
                return false;
            }
            // 玩家默认信息
            PlayerData playerData = new PlayerData();
            // 序列化
            string data = Js.Serialize(playerData);
            // 写入数据库
            string sql = string.Format("insert into player set id ='{0}',data='{1}';", id, data);
            try {
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e ) {
                Console.WriteLine("[数据库]玩家写入失败!"+e .Message);
                return false;
            } 
        }

        // 检查用户的密码
        public static bool CheckPassword(string id, string pw) {
            // 安全检查
            if (!DbManager.IsSafeString(id)) {
                Console.WriteLine("[数据库]用户注册失败,安全检查未通过");
                return false;
            }
            if (!DbManager.IsSafeString(pw)) {
                Console.WriteLine("[数据库]用户注册失败,安全检查未通过");
                return false;
            }
            // 查询
            string sql = string.Format("select * from account where id ='{0}' and pw '{1}'", id, pw);
            Console.WriteLine(sql );
            try {
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool hasRows = dataReader.HasRows;
                dataReader.Close();
                return hasRows;
            }
            catch (Exception e ) {
                Console.WriteLine("[数据库]密码检查错误,不存在此密码");
                return false;
            }
        }

        // 从数据库中读取玩家数据
        public static PlayerData GetPlayerData(string id) {
            if (!DbManager.IsSafeString(id)) {
                Console.WriteLine("[数据库]获取玩家信息失败,安全检查未通过");
                return null;
            }
            string sql = string.Format("select * from player where id = '{0}';", id);
            Console.WriteLine(sql);
            try {
                // 查询
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                if (!dataReader.HasRows) {
                    dataReader.Close();
                    return null;
                }
                // 获取数据
                dataReader.Read();
                string data = dataReader.GetString("data");
                // 反序列化
                PlayerData playerData = Js.Deserialize<PlayerData>(data);
                dataReader.Close();
                return playerData;
            }
            catch (Exception e) {
                Console.WriteLine("[数据库]获取玩家数据失败!"+e .Message);
                return null;
            }
        }

        // 更新玩家数据
        public static bool UpdatePlayerData(string id, PlayerData playerData) {
            // 序列化
            string data = Js.Serialize(playerData);
            // sql
            string sql = string.Format("update player set data ='{0}' where id ='{1}';", data, id);
            Console.WriteLine(sql);
            try {
                MySqlCommand cmd = new MySqlCommand(sql, mysql);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("[数据库]更新玩家数据失败!" + e.Message);
                return false;
            }
        }
    }
}
