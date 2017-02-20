using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _3D_reconstruction;

namespace _3D_reconstruction_Test
{
    [TestClass]
    public class PointComparerYTest
    {
        // Перевірка чи даний модуль не получає значення null
        [TestMethod]
        public void Point3d_ToStringIsNotNull()
        {
            var point3 = new Point3d(1,2,3);
            var emp = point3.ToString();
            Assert.IsNotNull(emp);
        }

        // Перевірка чи даний модуль приймає значення
        [TestMethod]
        public void Point3d_ToStringIsCorrect()
        {
            var point3 = new Point3d(1, 2, 3);
            var emp = point3.ToString();
            Assert.AreEqual(emp,"1 2 3");
        }

        // Перевірка чи даний модуль получає значння коли дані однакові
        [TestMethod]
        public void PointComparer_MustReturn0()
        {
            var point1 = new Point(1, 3);
            var point2 = new Point(1, 3);
            var result = new PointComparer().Compare(point1, point2);
            Assert.AreEqual(result, 0);
        }

        // Перевірка чи даний модуль получає значння коли дані більші, а другі менші
        [TestMethod]
        public void PointComparer_MustReturn1()
        {
            var point1 = new Point(3, 1);
            var point2 = new Point(1, 3);
            var result = new PointComparer().Compare(point1, point2);
            Assert.AreEqual(result, 0);
        }

        // Перевірка чи даний модуль получає значння коли дані менші, а другі більші
        [TestMethod]
        public void PointComparer_MustReturn2()
        {
            var point1 = new Point(1, 3);
            var point2 = new Point(3, 1);
            var result = new PointComparer().Compare(point1, point2);
            Assert.AreEqual(result, 0);
        }
    }
}
