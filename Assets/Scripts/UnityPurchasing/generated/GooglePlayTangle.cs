// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("B7U2FQc6MT4dsX+xwDo2NjYyNzSlNNQD1T82P1ycXn4ztYmtmvCzegi0vR0q60gG9WiQrpGe5Vvpq3r66khylhw8nQQcVGauhFmGMG4+IZi/jKL/803n5M0rgGsmcu3oFSNe4bU2ODcHtTY9NbU2Nje7a3DUET8hIsirNdgRI5tOvxmv0NAbU5tkmv1fW7La01Z0D5OvYlJCYrOnjzTe0EG5wXOPKzOBEFRfrhG4LAm/k4S7hP+UaEe/yIZBX5hJF6tBQlP1w+2aZcJKk9zM9+aoob1RLJj9vczX+4ofnWe+W94gxu3mRw/T/LcoWDL7NLx7JwctK6OrXCzuwtGnGsBBfPNDCgMnupkHCS93XuocZ3yKqJIoq3jhztBDKDoDIDU0Njc2");
        private static int[] order = new int[] { 0,10,13,6,4,10,7,8,9,13,13,11,12,13,14 };
        private static int key = 55;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
