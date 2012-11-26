using System;

// originally from here: https://gist.github.com/767532, but adapted

namespace Drash.Common.Api
{
    public class SolarInfo
    {
        public DateTimeOffset Sunrise { get; private set; }
        public DateTimeOffset Sunset { get; private set; }

        private SolarInfo() { }

        public static SolarInfo ForDate(double latitude, double longitude, DateTimeOffset date)
        {
            var info = new SolarInfo();

            var year = date.Year;
            var month = date.Month;
            var day = date.Day;

            //*****	Calculate the time of sunrise			
            var JD = CalcJulianDay(year, month, day);
            var doy = CalcDayOfYear(month, day, IsLeapYear(year));

            // Calculate sunrise for this date
            // if no sunrise is found, set flag nosunrise
            var riseTimeGmt = CalcSuntimeUtc(false, JD, latitude, longitude);
            if (IsNumber(riseTimeGmt)) {
                riseTimeGmt = CalcSuntimeUtc(false, JD + riseTimeGmt / 1440.0, latitude, longitude);
                info.Sunrise = date.Date.AddMinutes(riseTimeGmt);
            }
            else {
                // if Northern hemisphere and spring or summer, OR  
                // if Southern hemisphere and fall or winter, use 
                // previous sunrise and next sunset

                if (((latitude > 66.4) && (doy > 79) && (doy < 267)) ||
                   ((latitude < -66.4) && ((doy < 83) || (doy > 263)))) {
                    var newjd = FindRecentSuntime(false, JD, latitude, longitude);
                    var newtime = CalcSuntimeUtc(false, newjd, latitude, longitude);

                    if (newtime > 1440) {
                        newtime -= 1440;
                        newjd += 1.0;
                    }
                    if (newtime < 0) {
                        newtime += 1440;
                        newjd -= 1.0;
                    }

                    info.Sunrise = ConvertToDate(newtime, newjd);
                }

                // if Northern hemisphere and fall or winter, OR 
                // if Southern hemisphere and spring or summer, use 
                // next sunrise and previous sunset

                else if (((latitude > 66.4) && ((doy < 83) || (doy > 263))) ||
                    ((latitude < -66.4) && (doy > 79) && (doy < 267))) {
                    var newjd = FindRecentSuntime(false, JD, latitude, longitude);
                    var newtime = CalcSuntimeUtc(false, newjd, latitude, longitude);

                    if (newtime > 1440) {
                        newtime -= 1440;
                        newjd += 1.0;
                    }
                    if (newtime < 0) {
                        newtime += 1440;
                        newjd -= 1.0;
                    }

                    info.Sunrise = ConvertToDate(newtime, newjd);
                }
            }

            // Calculate sunset for this date
            // if no sunset is found, set flag nosunset
            var setTimeGmt = CalcSuntimeUtc(true, JD, latitude, longitude);
            if (IsNumber(setTimeGmt)) {
                setTimeGmt = CalcSuntimeUtc(true, JD + setTimeGmt / 1440.0, latitude, longitude);
                info.Sunset = date.Date.AddMinutes(setTimeGmt);
            }
            else {
                // if Northern hemisphere and spring or summer, OR
                // if Southern hemisphere and fall or winter, use 
                // previous sunrise and next sunset

                if (((latitude > 66.4) && (doy > 79) && (doy < 267)) ||
                   ((latitude < -66.4) && ((doy < 83) || (doy > 263)))) {
                    var newjd = FindNextSuntime(true, JD, latitude, longitude);
                    var newtime = CalcSuntimeUtc(true, newjd, latitude, longitude);

                    if (newtime > 1440) {
                        newtime -= 1440;
                        newjd += 1.0;
                    }
                    if (newtime < 0) {
                        newtime += 1440;
                        newjd -= 1.0;
                    }

                    info.Sunset = ConvertToDate(newtime, newjd);
                }

                // if Northern hemisphere and fall or winter, OR
                // if Southern hemisphere and spring or summer, use 
                // next sunrise and last sunset

                else if (((latitude > 66.4) && ((doy < 83) || (doy > 263))) ||
                    ((latitude < -66.4) && (doy > 79) && (doy < 267))) {
                    var newjd = FindNextSuntime(true, JD, latitude, longitude);
                    var newtime = CalcSuntimeUtc(true, newjd, latitude, longitude);

                    if (newtime > 1440) {
                        newtime -= 1440;
                        newjd += 1.0;
                    }
                    if (newtime < 0) {
                        newtime += 1440;
                        newjd -= 1.0;
                    }

                    info.Sunset = ConvertToDate(newtime, newjd);
                }

            }

            return info;
        }

        // This is inspired by timeStringShortAMPM from the original source.
        /// <summary>Note: This treats fractional julian days as whole days, so the minutes portion of the julian day will be replaced with the value of the minutes parameter.</summary>
        private static DateTime ConvertToDate(double minutes, double JD)
        {
            var julianday = JD;
            var floatHour = minutes / 60.0;
            var hour = Math.Floor(floatHour);
            var floatMinute = 60.0 * (floatHour - Math.Floor(floatHour));
            var minute = Math.Floor(floatMinute);
            var floatSec = 60.0 * (floatMinute - Math.Floor(floatMinute));
            var second = Math.Floor(floatSec + 0.5);

            minute += (second >= 30) ? 1 : 0;

            if (minute >= 60) {
                minute -= 60;
                hour++;
            }

            if (hour > 23) {
                hour -= 24;
                julianday += 1.0;
            }

            if (hour < 0) {
                hour += 24;
                julianday -= 1.0;
            }

            return CalcDayFromJd(julianday).Add(new TimeSpan(0, (int)hour, (int)minute, (int)second));
        }

        private static DateTime CalcDayFromJd(double jd)
        {
            var z = Math.Floor(jd + 0.5);
            var f = (jd + 0.5) - z;

            double A = 0;
            if (z < 2299161) {
                A = z;
            }
            else {
                var alpha = Math.Floor((z - 1867216.25) / 36524.25);
                A = z + 1 + alpha - Math.Floor(alpha / 4);
            }

            var B = A + 1524;
            var C = Math.Floor((B - 122.1) / 365.25);
            var D = Math.Floor(365.25 * C);
            var E = Math.Floor((B - D) / 30.6001);

            var day = B - D - Math.Floor(30.6001 * E) + f;
            var month = (E < 14) ? E - 1 : E - 13;
            var year = (month > 2) ? C - 4716 : C - 4715;

            return new DateTime((int)year, (int)month, (int)day, 0, 0, 0, DateTimeKind.Utc);
        }

        private static bool IsLeapYear(int yr)
        {
            return ((yr % 4 == 0 && yr % 100 != 0) || yr % 400 == 0);
        }


        //*********************************************************************/

        private static bool IsNumber(double inputVal)
        {
            return !double.IsNaN(inputVal);
        }

        private static double RadToDeg(double angleRad)
        {
            return (180.0 * angleRad / Math.PI);
        }


        // Convert degree angle to radians
        private static double DegToRad(double angleDeg)
        {
            return (Math.PI * angleDeg / 180.0);
        }

        //***********************************************************************/
        //* Name:    calcDayOfYear								*/
        //* Type:    Function									*/
        //* Purpose: Finds numerical day-of-year from mn, day and lp year info  */
        //* Arguments:										*/
        //*   month: January = 1								*/
        //*   day  : 1 - 31									*/
        //*   lpyr : 1 if leap year, 0 if not						*/
        //* Return value:										*/
        //*   The numerical day of year							*/
        //***********************************************************************/
        private static int CalcDayOfYear(int mn, int dy, bool lpyr)
        {
            var k = (lpyr ? 1 : 2);
            var doy = Math.Floor((275d * mn) / 9d) - k * Math.Floor((mn + 9d) / 12d) + dy - 30;
            return (int)doy;
        }


        //***********************************************************************/
        //* Name:    calcJD									*/
        //* Type:    Function									*/
        //* Purpose: Julian day from calendar day						*/
        //* Arguments:										*/
        //*   year : 4 digit year								*/
        //*   month: January = 1								*/
        //*   day  : 1 - 31									*/
        //* Return value:										*/
        //*   The Julian day corresponding to the date					*/
        //* Note:											*/
        //*   Number is returned for start of day.  Fractional days should be	*/
        //*   added later.									*/
        //***********************************************************************/
        private static double CalcJulianDay(int year, int month, int day)
        {
            if (month <= 2) {
                year -= 1;
                month += 12;
            }

            var A = Math.Floor(year / 100d);
            var B = 2 - A + Math.Floor(A / 4d);

            var JD = Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1.0)) + day + B - 1524.5;

            return JD;
        }

        //***********************************************************************/
        //* Name:    ConvertJulianDayToCenturiesSinceJ2000							*/
        //* Type:    Function									*/
        //* Purpose: convert Julian Day to centuries since J2000.0.			*/
        //* Arguments:										*/
        //*   jd : the Julian Day to convert						*/
        //* Return value:										*/
        //*   the T value corresponding to the Julian Day				*/
        //***********************************************************************/
        private static double ConvertJulianDayToCenturiesSinceJ2000(double jd)
        {
            var T = (jd - 2451545.0) / 36525.0;
            return T;
        }


        //***********************************************************************/
        //* Name:    calGeomMeanLongSun							*/
        //* Type:    Function									*/
        //* Purpose: calculate the Geometric Mean Longitude of the Sun		*/
        //* Arguments:										*/
        //*   t : number of Julian centuries since J2000.0				*/
        //* Return value:										*/
        //*   the Geometric Mean Longitude of the Sun in degrees			*/
        //***********************************************************************/
        private static double CalcGeomMeanLongSun(double t)
        {
            var L0 = 280.46646 + t * (36000.76983 + 0.0003032 * t);
            while (L0 > 360.0) {
                L0 -= 360.0;
            }
            while (L0 < 0.0) {
                L0 += 360.0;
            }
            return L0;		// in degrees
        }

        //***********************************************************************/
        //* Name:    calGeomAnomalySun							*/
        //* Type:    Function									*/
        //* Purpose: calculate the Geometric Mean Anomaly of the Sun		*/
        //* Arguments:										*/
        //*   t : number of Julian centuries since J2000.0				*/
        //* Return value:										*/
        //*   the Geometric Mean Anomaly of the Sun in degrees			*/
        //***********************************************************************/
        private static double CalcGeomMeanAnomalySun(double t)
        {
            var M = 357.52911 + t * (35999.05029 - 0.0001537 * t);
            return M;		// in degrees
        }


        //***********************************************************************/
        //* Name:    calcEccentricityEarthOrbit						*/
        //* Type:    Function									*/
        //* Purpose: calculate the eccentricity of earth's orbit			*/
        //* Arguments:										*/
        //*   t : number of Julian centuries since J2000.0				*/
        //* Return value:										*/
        //*   the unitless eccentricity							*/
        //***********************************************************************/
        private static double CalcEccentricityEarthOrbit(double t)
        {
            var e = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);
            return e;		// unitless
        }

        //***********************************************************************/
        //* Name:    calcSunEqOfCenter							*/
        //* Type:    Function									*/
        //* Purpose: calculate the equation of center for the sun			*/
        //* Arguments:										*/
        //*   t : number of Julian centuries since J2000.0				*/
        //* Return value:										*/
        //*   in degrees										*/
        //***********************************************************************/
        private static double CalcSunEqOfCenter(double t)
        {
            var m = CalcGeomMeanAnomalySun(t);

            var mrad = DegToRad(m);
            var sinm = Math.Sin(mrad);
            var sin2m = Math.Sin(mrad + mrad);
            var sin3m = Math.Sin(mrad + mrad + mrad);

            return sinm * (1.914602 - t * (0.004817 + 0.000014 * t)) + sin2m * (0.019993 - 0.000101 * t) + sin3m * 0.000289;
        }

        //***********************************************************************/
        //* Name:    calcSunTrueLong								*/
        //* Type:    Function									*/
        //* Purpose: calculate the true longitude of the sun				*/
        //* Arguments:										*/
        //*   t : number of Julian centuries since J2000.0				*/
        //* Return value:										*/
        //*   sun's true longitude in degrees						*/
        //***********************************************************************/
        private static double CalcSunTrueLong(double t)
        {
            return CalcGeomMeanLongSun(t) + CalcSunEqOfCenter(t);
        }


        //***********************************************************************/
        //* Name:    calcSunApparentLong							*/
        //* Type:    Function									*/
        //* Purpose: calculate the apparent longitude of the sun			*/
        //* Arguments:										*/
        //*   t : number of Julian centuries since J2000.0				*/
        //* Return value:										*/
        //*   sun's apparent longitude in degrees						*/
        //***********************************************************************/
        private static double CalcSunApparentLong(double t)
        {
            var o = CalcSunTrueLong(t);
            var omega = 125.04 - 1934.136 * t;
            return o - 0.00569 - 0.00478 * Math.Sin(DegToRad(omega));
        }

        //***********************************************************************/
        //* Name:    calcMeanObliquityOfEcliptic						*/
        //* Type:    Function									*/
        //* Purpose: calculate the mean obliquity of the ecliptic			*/
        //* Arguments:										*/
        //*   t : number of Julian centuries since J2000.0				*/
        //* Return value:										*/
        //*   mean obliquity in degrees							*/
        //***********************************************************************/
        private static double CalcMeanObliquityOfEcliptic(double t)
        {
            var seconds = 21.448 - t * (46.8150 + t * (0.00059 - t * (0.001813)));
            return 23.0 + (26.0 + (seconds / 60.0)) / 60.0;
        }



        //***********************************************************************/
        //* Name:    calcObliquityCorrection						*/
        //* Type:    Function									*/
        //* Purpose: calculate the corrected obliquity of the ecliptic		*/
        //* Arguments:										*/
        //*   t : number of Julian centuries since J2000.0				*/
        //* Return value:										*/
        //*   corrected obliquity in degrees						*/
        //***********************************************************************/
        private static double CalcObliquityCorrection(double t)
        {
            var e0 = CalcMeanObliquityOfEcliptic(t);
            var omega = 125.04 - 1934.136 * t;
            return e0 + 0.00256 * Math.Cos(DegToRad(omega));
        }

        //***********************************************************************/
        //* Name:    calcSunDeclination							*/
        //* Type:    Function									*/
        //* Purpose: calculate the declination of the sun				*/
        //* Arguments:										*/
        //*   t : number of Julian centuries since J2000.0				*/
        //* Return value:										*/
        //*   sun's declination in degrees							*/
        //***********************************************************************/
        private static double CalcSunDeclination(double t)
        {
            var e = CalcObliquityCorrection(t);
            var lambda = CalcSunApparentLong(t);

            var sint = Math.Sin(DegToRad(e)) * Math.Sin(DegToRad(lambda));
            return RadToDeg(Math.Asin(sint));
        }

        //***********************************************************************/
        //* Name:    calcEquationOfTime							*/
        //* Type:    Function									*/
        //* Purpose: calculate the difference between true solar time and mean	*/
        //*		solar time									*/
        //* Arguments:										*/
        //*   t : number of Julian centuries since J2000.0				*/
        //* Return value:										*/
        //*   equation of time in minutes of time						*/
        //***********************************************************************/
        private static double CalcEquationOfTime(double t)
        {
            var epsilon = CalcObliquityCorrection(t);
            var l0 = CalcGeomMeanLongSun(t);
            var e = CalcEccentricityEarthOrbit(t);
            var m = CalcGeomMeanAnomalySun(t);

            var y = Math.Tan(DegToRad(epsilon) / 2.0);
            y *= y;

            var sin2L0 = Math.Sin(2.0 * DegToRad(l0));
            var sinm = Math.Sin(DegToRad(m));
            var cos2L0 = Math.Cos(2.0 * DegToRad(l0));
            var sin4L0 = Math.Sin(4.0 * DegToRad(l0));
            var sin2M = Math.Sin(2.0 * DegToRad(m));

            var etime = y * sin2L0 - 2.0 * e * sinm + 4.0 * e * y * sinm * cos2L0
                    - 0.5 * y * y * sin4L0 - 1.25 * e * e * sin2M;

            return RadToDeg(etime) * 4.0;	// in minutes of time
        }

        //***********************************************************************/
        //* Name:    calcHourAngleSunrise							*/
        //* Type:    Function									*/
        //* Purpose: calculate the hour angle of the sun at sunrise for the	*/
        //*			latitude								*/
        //* Arguments:										*/
        //*   lat : latitude of observer in degrees					*/
        //*	solarDec : declination angle of sun in degrees				*/
        //* Return value:										*/
        //*   hour angle of sunrise in radians						*/
        //***********************************************************************/
        private static double CalcHourAngleSuntime(bool sunset, double lat, double solarDec)
        {
            var latRad = DegToRad(lat);
            var sdRad = DegToRad(solarDec);
            var ha = (Math.Acos(Math.Cos(DegToRad(90.833)) / (Math.Cos(latRad) * Math.Cos(sdRad)) - Math.Tan(latRad) * Math.Tan(sdRad)));
            return sunset ? ha : -ha;		// in radians
        }

        //***********************************************************************/
        //* Name:    calcSunriseUTC								*/
        //* Type:    Function									*/
        //* Purpose: calculate the Universal Coordinated Time (UTC) of sunrise	*/
        //*			for the given day at the given location on earth	*/
        //* Arguments:										*/
        //*   JD  : julian day									*/
        //*   latitude : latitude of observer in degrees				*/
        //*   longitude : longitude of observer in degrees				*/
        //* Return value:										*/
        //*   time in minutes from zero Z							*/
        //***********************************************************************/
        private static double CalcSuntimeUtc(bool sunset, double JD, double latitude, double longitude)
        {
            var t = ConvertJulianDayToCenturiesSinceJ2000(JD);
            var eqTime = CalcEquationOfTime(t);
            var solarDec = CalcSunDeclination(t);
            var hourAngle = CalcHourAngleSuntime(sunset, latitude, solarDec);
            var delta = longitude - RadToDeg(hourAngle);
            return 720 - (4 * delta) - eqTime; // in minutes
        }


        //***********************************************************************/
        //* Name:    findRecentSunrise							*/
        //* Type:    Function									*/
        //* Purpose: calculate the julian day of the most recent sunrise		*/
        //*		starting from the given day at the given location on earth	*/
        //* Arguments:										*/
        //*   JD  : julian day									*/
        //*   latitude : latitude of observer in degrees				*/
        //*   longitude : longitude of observer in degrees				*/
        //* Return value:										*/
        //*   julian day of the most recent sunrise					*/
        //***********************************************************************/
        private static double FindRecentSuntime(bool sunset, double jd, double latitude, double longitude)
        {
            var julianday = jd;

            var time = CalcSuntimeUtc(sunset, julianday, latitude, longitude);
            while (!IsNumber(time)) {
                julianday -= 1.0;
                time = CalcSuntimeUtc(sunset, julianday, latitude, longitude);
            }

            return julianday;
        }


        //***********************************************************************/
        //* Name:    findNextSunrise								*/
        //* Type:    Function									*/
        //* Purpose: calculate the julian day of the next sunrise			*/
        //*		starting from the given day at the given location on earth	*/
        //* Arguments:										*/
        //*   JD  : julian day									*/
        //*   latitude : latitude of observer in degrees				*/
        //*   longitude : longitude of observer in degrees				*/
        //* Return value:										*/
        //*   julian day of the next sunrise						*/
        //***********************************************************************/
        private static double FindNextSuntime(bool sunset, double jd, double latitude, double longitude)
        {
            var julianday = jd;

            var time = CalcSuntimeUtc(sunset, julianday, latitude, longitude);
            while (!IsNumber(time)) {
                julianday += 1.0;
                time = CalcSuntimeUtc(sunset, julianday, latitude, longitude);
            }

            return julianday;
        }
    }
}