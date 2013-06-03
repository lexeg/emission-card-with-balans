using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CRT530Library
{
    public enum TypeMessages { UNDEFINED = Int32.MinValue, ERR = -1, OK = 0 };

    public enum TypeDispense
    {
        StopSensor3 = 1,
        StopSensor2 = 6,
        LeaveSensor3 = 2,
        StopSensor1 = 3,
        LeaveSensor2 = 7,
        ToDoor = 4,
        OutDoor = 0
    }

    class ClassCRT530
    {
        //private const Int32 ERR = -1;
        //private const Int32 OK = 0;

        private const Int32 NOCARD = 1;
        private const Int32 ERRCARD = 2;
        private const Int32 ERRRDCARD = 3;
        private const Int32 ERRWRCARD = 4;
        private const Int32 ERRCARDSEC = 5;
        private const Int32 ERRCARDKEY = 6;
        private const Int32 ERRCARDLOCKED = 7;
        private const Int32 ERRMSG = 8;
        private const Int32 ERRRFCARD = 9;
        private const Int32 ERRFORMAT = 10;
        private const Int32 ERROVERFLOW = 11;
        private const Int32 UNKNOWCARD = 12;

        private const Int32 ERRCARDPOSITION = 14;


        private const Int32 PAC_ADDRESS = 1021;

        private const Int32 ENQ = 0x05;
        private const Int32 ACK = 0x06;
        private const Int32 NAK = 0x15;
        private const Int32 EOT = 0x04;
        private const Int32 CAN = 0x18;
        private const Int32 STX = 0x02;
        private const Int32 ETX = 0x03;
        private const Int32 US = 0x1F;

        private String comPort = "";
        private Byte baudrate = 5;                                                              //так как работает только на этой скорости
        public IntPtr handle = IntPtr.Zero;
        private TypeMessages resultOp = TypeMessages.UNDEFINED;
        private Byte _Status0 = 255, _Status1 = 255, _Status2 = 255, _Status3 = 255;
        private Boolean isConnected = false;

        public delegate void myDelegate(String obj);
        public myDelegate logging;

        [DllImport("CRT_530.dll", CharSet = CharSet.None, SetLastError = true)]
        static extern int GetSysVerion(System.IntPtr ComHandle, [In, Out]char[] strVerion);

        [DllImport("CRT_530.dll", CharSet = CharSet.None, SetLastError = true)]
        static extern System.IntPtr CommOpen(String Port);

        [DllImport("CRT_530.dll", CharSet = CharSet.None, SetLastError = true)]
        static extern System.IntPtr CommOpenWithBaut(String Port, Byte _Baute);

        [DllImport("CRT_530.dll", CharSet = CharSet.None, SetLastError = true)]
        static extern int CommClose(System.IntPtr ComHandle);

        [DllImport("CRT_530.dll", CharSet = CharSet.None)]
        static extern int CommSetting(System.IntPtr ComHandle, String ComSeting);

        ////////////////////////////////////////////////////////////////////////////////////////
        [DllImport("CRT_530.dll", CharSet = CharSet.None)]
        static extern int CRT530_Dispense(System.IntPtr ComHandle);
        [DllImport("CRT_530.dll", CharSet = CharSet.None)]
        static extern int CRT530_Capture(System.IntPtr ComHandle);
        [DllImport("CRT_530.dll", CharSet = CharSet.None)]
        //static extern int CRT530_GetRF(System.IntPtr ComHandle, [In, Out] Byte[] _Status1, [In, Out] Byte[] _Status2, [In, Out] Byte[] _Status3);
        static extern int CRT530_GetRF(System.IntPtr ComHandle, ref Byte _Status1, ref Byte _Status2, ref Byte _Status3);
        [DllImport("CRT_530.dll", CharSet = CharSet.None)]
        static extern int CRT530_GetAP(System.IntPtr ComHandle, ref Byte _Status1, ref Byte _Status2, ref Byte _Status3, ref Byte _Status4);
        [DllImport("CRT_530.dll", CharSet = CharSet.None)]
        static extern int CRT530_SetComm(System.IntPtr ComHandle, Byte _data);
        [DllImport("CRT_530.dll", CharSet = CharSet.None)]
        static extern int CRT530_Reset(System.IntPtr ComHandle);
        [DllImport("CRT_530.dll", CharSet = CharSet.None)]
        static extern int CRT530_PreDispense(System.IntPtr ComHandle, Byte _Address);
        [DllImport("CRT_530.dll", CharSet = CharSet.None)]
        static extern int CRT530_IN(System.IntPtr ComHandle, Byte _CardIn);
        [DllImport("CRT_530.dll", CharSet = CharSet.None)]
        static extern int CRT530_SI(System.IntPtr ComHandle, ref Byte _CardIn);

        public String ComPort
        {
            get { return comPort; }
            set { comPort = value; }
        }

        public Byte BaudRate
        {
            get { return baudrate; }
            set { baudrate = value; }
        }

        public Boolean IsConnected
        {
            get { return isConnected; }
        }

        public TypeMessages ResultOperation
        {
            get { return resultOp; }
            set { resultOp = value; }
        }

        public Boolean OpenPort(String comPort, Byte baudrate)
        {
            try
            {
                isConnected = false;
                handle = CommOpenWithBaut(comPort, baudrate);
                if (handle != IntPtr.Zero)
                {
                    isConnected = true;
                    return isConnected;
                }
                if (Marshal.GetLastWin32Error() != 0)
                {
                    //logging("OpenPort; Error: " + Marshal.GetLastWin32Error());
                    //throw new CRT530Exception(Marshal.GetLastWin32Error());
                    throw new CRT530Exception(String.Format("OpenPort; Error: {0}", Marshal.GetLastWin32Error()));
                }
            }
            catch (CRT530Exception crtEx)
            {
                throw crtEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return isConnected;
        }

        public Boolean ClosePort()
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    ResultOperation = (TypeMessages)CommClose(handle);
                    logging(String.Format("ClosePort; Comm close: {0}", ResultOperation.ToString()));
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        //logging("ClosePort; Error: " + Marshal.GetLastWin32Error());
                        throw new CRT530Exception(String.Format("ClosePort; Error: {0}", Marshal.GetLastWin32Error()));
                    }
                    switch (ResultOperation)
                    {
                        case TypeMessages.OK:
                            {
                                return true;
                            }
                        case TypeMessages.ERR:
                            {
                                return false;
                            }
                        default:
                            {
                                return false;
                            }
                    }
                }
            }
            catch (CRT530Exception crtEx)
            {
                throw crtEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                handle = IntPtr.Zero;
                isConnected = false;
            }
            return false;
        }

        public void DispenceCard()
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    ResultOperation = (TypeMessages)CRT530_Dispense(handle);
                    logging(String.Format("DispenceCard: {0}", ResultOperation.ToString()));
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        //logging("DispenceCard; Error: " + Marshal.GetLastWin32Error());
                        //throw new CRT530Exception(String.Format("DispenceCard; Error: {0}", Marshal.GetLastWin32Error()));
                    }
                }
            }
            catch (CRT530Exception crtEx)
            {
                throw crtEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void CaptureCard()
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    ResultOperation = (TypeMessages)CRT530_Capture(handle);
                    logging(String.Format("CaptureCard: {0}", ResultOperation.ToString()));
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        //logging("CaptureCard; Error: " + Marshal.GetLastWin32Error());
                        //throw new CRT530Exception(String.Format("DispenceCard; Error: {0}", Marshal.GetLastWin32Error()));
                    }
                }
            }
            catch (CRT530Exception crtEx)
            {
                throw crtEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Int32 GetRF()
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    _Status1 = 255;
                    _Status2 = 255;
                    _Status3 = 255;
                    ResultOperation = (TypeMessages)CRT530_GetRF(handle, ref _Status1, ref _Status2, ref _Status3);
                    logging(String.Format("GetRF: {0}; {1}; {2}. Result: {3}",
                        _Status1,
                        _Status2,
                        _Status3,
                        ResultOperation.ToString()));
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        //logging("GetRF; Error: " + Marshal.GetLastWin32Error());
                        throw new CRT530Exception(String.Format("GetRF; Error: {0}", Marshal.GetLastWin32Error()));
                    }
                    if (ResultOperation == TypeMessages.ERR)
                    {
                        //return Int32.MaxValue;
                        //throw new CRT530Exception(String.Format("GetRF; Произошла ошибка при получении статусов устройства"));
                    }
                    Int32 status = 0;
                    status = (status << 4) | _Status1;
                    status = (status << 4) | _Status2;
                    status = (status << 4) | _Status3;
                    logging(String.Format("GetRF: {0:X4}", status));
                    return status;
                }
            }
            catch (CRT530Exception crtEx)
            {
                throw crtEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Int32.MaxValue;
        }

        public Int32 GetAP()
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    _Status0 = 255;
                    _Status1 = 255;
                    _Status2 = 255;
                    _Status3 = 255;
                    ResultOperation = (TypeMessages)CRT530_GetAP(handle, ref _Status0, ref _Status1, ref _Status2, ref _Status3);
                    logging(String.Format("GetAP: {0}; {1}; {2}; {3}. Result: {4}",
                        _Status0,
                        _Status1,
                        _Status2,
                        _Status3,
                        ResultOperation.ToString()));
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        //logging("GetAP; Error: " + Marshal.GetLastWin32Error());
                        //throw new CRT530Exception(String.Format("GetAP; Error: {0}", Marshal.GetLastWin32Error()));
                    }
                    if (ResultOperation == TypeMessages.ERR)
                    {
                        //return Int32.MaxValue;
                        //throw new CRT530Exception(String.Format("GetAP; Произошла ошибка при получении статусов устройства"));
                    }
                    Int32 status = 0;
                    status = (status << 4) | _Status0;
                    status = (status << 4) | _Status1;
                    status = (status << 4) | _Status2;
                    status = (status << 4) | _Status3;
                    logging(String.Format("GetAP: {0:X4}", status));
                    return status;
                }
            }
            catch (CRT530Exception crtEx)
            {
                throw crtEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Int32.MaxValue;
        }

        public void ResetDispenser()
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    ResultOperation = (TypeMessages)CRT530_Reset(handle);
                    logging(String.Format("ResetDispenser: {0}", ResultOperation.ToString()));
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        //logging("ResetDispenser; Error: " + Marshal.GetLastWin32Error());
                        //throw new CRT530Exception(String.Format("ResetDispenser; Error: {0}", Marshal.GetLastWin32Error()));
                    }
                }
            }
            catch (CRT530Exception crtEx)
            {
                throw crtEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void PreDispenseCard(TypeDispense _Address)
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    ResultOperation = (TypeMessages)CRT530_PreDispense(handle, (byte)_Address);
                    logging(String.Format("PreDispenseCard: {0}", ResultOperation.ToString()));
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        //logging("PreDispenseCard; Error: " + Marshal.GetLastWin32Error());
                        //throw new CRT530Exception(String.Format("PreDispenseCard; Error: {0}", Marshal.GetLastWin32Error()));
                    }
                }
            }
            catch (CRT530Exception crtEx)
            {
                throw crtEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void IN(Byte _CardIn)
        {
            try
            {
                //Есть 3 варианта
                if (handle != IntPtr.Zero)
                {
                    ResultOperation = (TypeMessages)CRT530_IN(handle, _CardIn);
                    logging(String.Format("IN: {0}; Result: {1};", _CardIn, ResultOperation.ToString()));
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        //logging("IN; Error: " + Marshal.GetLastWin32Error());
                        throw new CRT530Exception(String.Format("IN; Error: {0}", Marshal.GetLastWin32Error()));
                    }
                }
            }
            catch (CRT530Exception crtEx)
            {
                throw crtEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        //не работает, исправить
        public void SI()
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    Byte _CardIn = 255;
                    ResultOperation = (TypeMessages)CRT530_SI(handle, ref _CardIn);
                    logging(String.Format("SI: {0}. Result: {1}", _CardIn, ResultOperation.ToString()));
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        //logging("SI; Error: " + Marshal.GetLastWin32Error());
                        throw new CRT530Exception(String.Format("SI; Error: {0}", Marshal.GetLastWin32Error()));
                    }
                }
            }
            catch (CRT530Exception crtEx)
            {
                throw crtEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}