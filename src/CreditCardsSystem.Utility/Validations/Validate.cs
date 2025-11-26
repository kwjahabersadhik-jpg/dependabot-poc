using System.Text.RegularExpressions;

namespace CreditCardsSystem.Utility.Validations
{
    public class Validate
    {
        public bool IsValidCivilId(string civilID)
        {
            try
            {
                if (string.IsNullOrEmpty(civilID))
                    return false;

                if (civilID.Length == 12)
                {
                    int tmp = 0;
                    int Chk = 0;

                    tmp = tmp + Convert.ToInt32(civilID.Substring(0, 1)) * 2;
                    tmp = tmp + Convert.ToInt32(civilID.Substring(1, 1)) * 1;
                    tmp = tmp + Convert.ToInt32(civilID.Substring(2, 1)) * 6;
                    tmp = tmp + Convert.ToInt32(civilID.Substring(3, 1)) * 3;
                    tmp = tmp + Convert.ToInt32(civilID.Substring(4, 1)) * 7;
                    tmp = tmp + Convert.ToInt32(civilID.Substring(5, 1)) * 9;
                    tmp = tmp + Convert.ToInt32(civilID.Substring(6, 1)) * 10;
                    tmp = tmp + Convert.ToInt32(civilID.Substring(7, 1)) * 5;
                    tmp = tmp + Convert.ToInt32(civilID.Substring(8, 1)) * 8;
                    tmp = tmp + Convert.ToInt32(civilID.Substring(9, 1)) * 4;
                    tmp = tmp + Convert.ToInt32(civilID.Substring(10, 1)) * 2;
                    Chk = Convert.ToInt32(civilID.Substring(11, 1));

                    tmp = 11 - (tmp % 11);
                    if (Convert.ToInt32(civilID.Substring(0, 1)) < 2)
                        return false;

                    //Checking for Date Of Birth for Valid Date
                    int yy = 0, mm = 0, dd = 0;
                    yy = Convert.ToInt32(civilID.Substring(1, 2));
                    if (yy == 2)
                        yy += 1900;
                    else
                        yy += 2000;

                    mm = Convert.ToInt32(civilID.Substring(3, 2));
                    dd = Convert.ToInt32(civilID.Substring(5, 2));

                    if (yy != 0 && mm != 0 && dd != 0)
                    {
                        DateTime DOB = new DateTime(yy, mm, dd);
                    }
                    else
                        return false;

                    if ((tmp < 1) || (tmp > 9) || (tmp != Chk))
                        return false;
                    else
                        return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public DateTime GetDOBFromCivilId(string CivilID)
        {
            try
            {
                int yy = 0, mm = 0, dd = 0;
                yy = Convert.ToInt32(CivilID.Substring(1, 2));
                if (CivilID.Substring(0, 1) == "2")
                    yy += 1900;
                else
                    yy += 2000;
                mm = Convert.ToInt32(CivilID.Substring(3, 2));
                dd = Convert.ToInt32(CivilID.Substring(5, 2));

                DateTime DOB = DateTime.MinValue;
                if (yy != 0 && mm != 0 && dd != 0)
                    DOB = new DateTime(yy, mm, dd);
                return DOB;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public bool IsValidMobilePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return false;

            if (!long.TryParse(phoneNumber, out long _phoneNumber) || _phoneNumber <= 0)
                return false;

            try
            {
                return validateZAINPhoneNumber(phoneNumber)
                    || validateOoredooPhoneNumber(phoneNumber)
                    || validateSTCPhoneNumber(phoneNumber)
                    || validateVirginPhoneNumber(phoneNumber);
            }
            catch (System.Exception)
            {
                return false;
            }

        }

        #region validateMocPhoneNumber

        /// <summary>
        /// Validates MOC phone number.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public bool validateMocPhoneNumber(string phoneNumber)
        {
            bool output = false;
            long outLongValue;
            try
            {
                if (longTryParse(phoneNumber, out outLongValue) && phoneNumber.Length == 8 && !IsValidMobilePhoneNumber(phoneNumber))
                {
                    output = true;
                }
            }
            catch (System.Exception)
            {
                output = false;
            }

            return output;
        }

        #endregion validateMocPhoneNumber

        #region validateZAINPhoneNumber

        /// <summary>
        /// Validates ZAIN phone number.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public bool validateZAINPhoneNumber(string phoneNumber)
        {
            //<Tariq Al-Mutairi > modified this function to make it read regular exprison from configuration file
            bool output = false;
            long outLongValue;

            try
            {
                if (longTryParse(phoneNumber, out outLongValue) && phoneNumber.Length == 8)
                {
                    //read the regular exrision that validate ZAIN mobile numbers
                    //from machine.config file located at C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\CONFIG\machine.config
                    string s = "(^9|^(44)|^(702))|(^(7[^70]))";  //System.Configuration.ConfigurationSettings.AppSettings["ZAIN"];
                    Regex ZAINMobilePrefix = new Regex(s);
                    if (ZAINMobilePrefix.IsMatch(phoneNumber))
                    {
                        output = true;
                    }
                }
            }
            catch (System.Exception)
            {
                output = false;
            }

            return output;
        }

        #endregion validateZAINPhoneNumber

        #region validateOoredooPhoneNumber

        /// <summary>
        /// Validates Ooredoo phone number.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public bool validateOoredooPhoneNumber(string phoneNumber)
        {
            //<Tariq Al-Mutairi > modified this function to make it read regular exprison from configuration file
            bool output = false;
            long outLongValue;

            try
            {
                if (longTryParse(phoneNumber, out outLongValue) && phoneNumber.Length == 8)
                {
                    string s = "(^6|^(70[^2])|^(77[^7])|^(50[^34])|^(51))";   //System.Configuration.ConfigurationSettings.AppSettings["Ooredoo"];
                    Regex OoredooMobilePrefix = new Regex(s);

                    if (OoredooMobilePrefix.IsMatch(phoneNumber))
                    {
                        output = true;
                    }
                }
            }
            catch (System.Exception)
            {
                output = false;
            }

            return output;
        }

        #endregion validateOoredooPhoneNumber

        #region validateSTCPhoneNumber

        /// <summary>
        /// Validates Ooredoo phone number.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public bool validateSTCPhoneNumber(string phoneNumber)
        {
            //<Tariq Al-Mutairi > modified this function to make it read regular exprison from configuration file
            bool output = false;
            long outLongValue;

            try
            {
                if (longTryParse(phoneNumber, out outLongValue) && phoneNumber.Length == 8)
                {
                    string s = "(^5)";   //System.Configuration.ConfigurationSettings.AppSettings["STC"];
                    Regex STCMobilePrefix = new Regex(s);

                    if (STCMobilePrefix.IsMatch(phoneNumber))
                    {
                        output = true;
                    }
                }
            }
            catch (System.Exception)
            {
                output = false;
            }

            return output;
        }

        #endregion validateSTCPhoneNumber

        #region validateVirginPhoneNumber
        /// <summary>
        /// Validates Virgin phone number.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public static bool validateVirginPhoneNumber(string phoneNumber)
        {

            bool output = false;
            long outLongValue;

            try
            {
                if (longTryParse(phoneNumber, out outLongValue) && phoneNumber.Length == 8)
                {
                    string s = "(^4)";
                    Regex VirginMobilePrefix = new Regex(s);
                    if (VirginMobilePrefix.IsMatch(phoneNumber))
                    {
                        output = true;
                    }
                }
            }
            catch (Exception)
            {
                output = false;
            }

            return output;
        }

        /// <summary>
        ///  this function used to mimic the behavior of long.TryParse (string s , out r); @ .Net 2.0
        /// </summary>
        /// <param name="s">Numeric string "Mobile Number"</param>
        /// <param name="r">Result r = s in long format</param>
        /// <returns> true if traslating ok false otherwise</returns>
        /// <Done by></>

        private static bool longTryParse(string s, out long r)
        {
            long x = 0;

            try
            {
                x = long.Parse(s);
            }
            catch (System.Exception e)
            {
                r = x;
                return false;
            }

            r = x;
            return true;
        }
        #endregion
    }
}
