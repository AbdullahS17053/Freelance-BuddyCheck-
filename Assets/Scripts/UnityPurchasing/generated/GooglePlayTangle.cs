// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("QBkTySQny8gXue1EqaDeb+MFAR7mVNf05tvQ3/xQnlAh29fX19PW1XPHGld1MM+cu7s4jNKXQBdWCZCCXgTR7wgZ1dxAA/zyKEgfR/6kDgTw5mJA2dzY2GJ2xjA7ou+cm2yXB+WxlJNpHUWSco2wVaDtSuGAr5LAinD8uqJeyCfAFhvfEB7gpi0Xtp93DMvhzTTmhvJ2QXv0CoE8uc6yVh9a86xOp+aPBwgodpvINE3XsdoB15W2ZRmfaOKs6kQunmVnINk4tWqPAEtHxsbE+mfzAFrTwtevW069sVTX2dbmVNfc1FTX19ZvdgBQF5UuaocIC9Ifs2pu19nnq1rXPm90yni7K6WSlcApvF908Iwvw8xgZ6NQJ9LDZ4G+7v3cD9TV19bX");
        private static int[] order = new int[] { 5,5,2,9,9,13,7,11,9,11,12,13,13,13,14 };
        private static int key = 214;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
