using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //c# kiểm tra zlib và nén và giải nén tệp smf trong gói cài đặt phiên bản Trung Quốc
            //Lần này, tôi, SmallPea, sẽ chỉ cho bạn cách giải nén và nén các gói dữ liệu smf (chuyển đổi giữa smf trong gói cài đặt và smf trong Android / data）

            Console.WriteLine("Bắt đầu xử lý...");
            Stopwatch time = new Stopwatch();
            time.Start();
            
            //Viết cuộc gọi của riêng bạn, chạy
            FileStream input = new FileStream(@".\zlib\dynamic.rsb.smf", FileMode.Open);
            SmfZlibUnCompressed(input, @".\zlib\dynamic.rsb.smf.jj");
            
            
            time.Stop();
            Console.WriteLine("Quá trình xử lý hoạt động đã kết thúc, tốn nhiều thời gian{0}mili giây",time.ElapsedMilliseconds.ToString());
            Console.ReadLine();
        }
        //C # nhận ra phiên bản nén và giải nén zlib của Trung Quốc
        private static  void SmfZlibCompressed(Stream  inputStream,CompressionLevel level, string outPutpath)
        {
            //Luồng đầu vào không bị đóng
            byte[] smfHeader = { 0xD4, 0xFE, 0xAD, 0xDE };//4 byte đầu tiên được cố định và 4 byte cuối cùng là kích thước tệp sau khi giải nén
            smfHeader = bytesArrayMerged(smfHeader,getFalseBytes(inputStream.Length,4));
            File.WriteAllBytes(outPutpath,bytesArrayMerged(smfHeader,ZlibCompressed(inputStream,level)));
              
        }
        private  static void SmfZlibUnCompressed(Stream inputStream, string outPutpath)
        {
            //Chỉ cần bỏ qua 8 byte đầu tiên, và sau đó giải nén zlib
            // Luồng đầu vào không bị đóng
            inputStream.Seek(8,SeekOrigin.Begin);
            byte[] zlibPartBytesBuff = new byte[1024];
            int trueReadlen = 0;
            MemoryStream zlibPart = new MemoryStream();
            while ((trueReadlen=inputStream.Read(zlibPartBytesBuff))!=0)
            {
                zlibPart.Write(zlibPartBytesBuff,0,trueReadlen);
            }
            File.WriteAllBytes(outPutpath,ZlibUnCompressed(zlibPart));
            
        }

        private static  byte[] bytesArrayMerged(byte[] ahead, byte[] hinder)
        {
            byte[] merged = new byte[ahead.Length + hinder.Length];
            for (int i = 0; i < ahead.Length; i++)
            {
                merged[i] = ahead[i];
            }
            for (int i = 0; i < hinder.Length; i++)
            {
                merged[ahead.Length + i] = hinder[i];
            }
            
            return merged;
        }
        private static  byte[] getAdler32Bytes(Stream input)
        {
            //Không đóng luồng đầu vào
            byte[] inputbytes = new byte[input.Length];
            input.Read(inputbytes);
            input.Seek(0, SeekOrigin.Begin);//Sau khi luồng được truyền vào, vì phương thức đã được gọi và đọc, con trỏ cần được đặt lại về 0 bên dưới
            uint adler32Value = Adler32Hash(inputbytes, 0, inputbytes.Length);
            String ValueHex = Convert.ToString(adler32Value,16);
            if (ValueHex.Length > 8)
            {
                throw new Exception("Độ dài Adler32 là bất thường");
            }
            ValueHex = stringMakeup(ValueHex, 4*2);
            byte[] result = new byte[4];
          
            for (int i = 0; i < 4; i++)
            {
              
                result[i] = Convert.ToByte(ValueHex.Substring(i*2,2),16);
            }
            return result;
        }
        public static uint Adler32Hash(byte[] bytesArray, int byteStart, int bytesToRead)
        {//Mã nguồn thuật toán Adler32
            int n;
            uint checksum = 1;
            uint s1 = checksum & 0xFFFF;
            uint s2 = checksum >> 16;

            while (bytesToRead > 0)
            {
                n = (3800 > bytesToRead) ? bytesToRead : 3800;
                bytesToRead -= n;
                while (--n >= 0)
                {
                    s1 = s1 + (uint)(bytesArray[byteStart++] & 0xFF);
                    s2 = s2 + s1;
                }
                s1 %= 65521;
                s2 %= 65521;
            }
            checksum = (s2 << 16) | s1;
            return checksum;
        }

        private static  byte[] ZlibCompressed(Stream  inputStream, CompressionLevel level)
        {
            //Không đóng luồng đầu vào
            // Mức nén mặc định -1 trong java thực sự là 6
            byte[] header = {0x78,0};
            if (level == CompressionLevel.Fastest)
            {//Nén nhanh mà không có mức chi tiết, nhấn 6
                header[1] = 0x9C;
            } else if (level == CompressionLevel.Optimal)
            {//Mức nén tối ưu 9
                header[1] = 0xDA;
            }else
            {// Không nén và thêm một số thông tin 5 byte vào phía trước
                header[1] = 0x01;
            }
            byte[] ender = getAdler32Bytes(inputStream);
           MemoryStream outStream = new MemoryStream();
            DeflateStream compresser = new DeflateStream(outStream ,level,true);
            int trueReadbytes = -1;
            byte[] buff = new byte[1024];
            while ((trueReadbytes = inputStream.Read(buff) )!= 0){
                compresser.Write(buff,0,trueReadbytes);
            }
            compresser.Close();
              return bytesArrayMerged(header,bytesArrayMerged(outStream.ToArray(),ender));
        }
        private static  byte[] ZlibUnCompressed(Stream  inputStream)
        {
            byte[] deflateData = new byte[inputStream.Length-6];
            inputStream.Seek(2,SeekOrigin.Begin);
            inputStream.Read(deflateData);
            MemoryStream inStream = new MemoryStream(deflateData);
            MemoryStream outStream = new MemoryStream();
            DeflateStream unCompresser = new DeflateStream(inStream, CompressionMode.Decompress,true);
            int trueReadbytes = -1;
            byte[] buff = new byte[1024];
            while ((trueReadbytes = unCompresser.Read(buff)) != 0)
            {
                outStream.Write(buff, 0, trueReadbytes);
            }

            return outStream.ToArray();
        }

        private  static string stringMakeup(string src,int length)
        {//Được sử dụng để điền vào chuỗi, sử dụng 0 để thêm chuyển tiếp
            if (src.Length > length)
            {
                throw new Exception("Độ dài đã đặt thấp hơn độ dài chuỗi ban đầu");
            }
            int needMakeupZeroSum = length - src.Length;
            StringBuilder re = new StringBuilder(src);
            for (int i = 0; i < needMakeupZeroSum; i++)
            {
                re.Insert(0, "0");
            }
            return re.ToString();
        }





        private static  long getTrueLength(byte []src)
        {//Nhận giá trị thực của byte được lật
            StringBuilder HexLen = new StringBuilder();
            foreach ( byte onebyte in src)
            {
                HexLen.Insert(0,Convert.ToString(onebyte,16));
            }
            return Convert.ToInt64(HexLen);
        }
        
        private static  byte[] getFalseBytes(long src ,int byteslength) 
        {
            byte[] srcfalsebytes = getFalseBytes(src);
            byte[] falsebytes = new byte[byteslength];
            if (srcfalsebytes.Length>byteslength)
            {
                throw new Exception("Đặt độ dài byte nhỏ hơn độ dài thực");
            }
            else
            {
                for (int i = 0; i < srcfalsebytes.Length; i++)
                {
                    falsebytes[i] = srcfalsebytes[i];
                }
            }
            return falsebytes;
        }
        private static  byte[] getFalseBytes(long src)
        {//Nhận chuỗi thập lục phân dài sau khi tách mảng byte
            // Độ dài byte độ dài bất kỳ
            string lengthhex = Convert.ToString(src, 16);
         
            if (lengthhex.Length / 2 != (lengthhex.Length / 2 * 2))
            {
                lengthhex = "0" + lengthhex;
            }
           
            byte[] refalsebytes = new byte[lengthhex .Length/2];
            for (int i = refalsebytes.Length; i>0; i--)
            {
                refalsebytes[i-1] = Convert.ToByte(lengthhex.Substring(lengthhex.Length-i*2, 2));
            }
           
            return refalsebytes;
        }


    }
 
}
