using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

namespace MLFaceLib
{
    public class EigenFaces
    {
        private List<Vector<double>> trainingImages = new List<Vector<double>>();
        private Vector<double> meanImage;
        private Matrix<double> eigenVectors;

        public void Train(string[] imagePaths)
        {
            List<Vector<double>> images = imagePaths.Select(LoadImageAsVector).ToList();
            meanImage = ComputeMeanImage(images);
            List<Vector<double>> adjustedImages = images.Select(img => img - meanImage).ToList();
            Matrix<double> covarianceMatrix = ComputeCovarianceMatrix(adjustedImages);
            eigenVectors = ComputeEigenVectors(covarianceMatrix);
        }

        public string Recognize(string imagePath)
        {
            Vector<double> inputVector = LoadImageAsVector(imagePath) - meanImage;
            Vector<double> projectedImage = ProjectOntoEigenfaces(inputVector);
            return FindClosestMatch(projectedImage);
        }

        private Vector<double> LoadImageAsVector(string path)
        {
            Bitmap bmp = new Bitmap(path);
            double[] pixels = new double[bmp.Width * bmp.Height];
            for (int x = 0; x < bmp.Width; x++)
                for (int y = 0; y < bmp.Height; y++)
                    pixels[y * bmp.Width + x] = bmp.GetPixel(x, y).R / 255.0;

            return Vector<double>.Build.Dense(pixels);
        }

        private Vector<double> ComputeMeanImage(List<Vector<double>> images)
        {
            return Vector<double>.Build.Dense(images.First().Count, i => images.Average(img => img[i]));
        }

        private Matrix<double> ComputeCovarianceMatrix(List<Vector<double>> images)
        {
            int dim = images[0].Count;
            Matrix<double> covariance = Matrix<double>.Build.Dense(dim, dim);
            foreach (var img in images)
                covariance += img.OuterProduct(img);
            return covariance / images.Count;
        }

        private Matrix<double> ComputeEigenVectors(Matrix<double> matrix)
        {
            var eigen = matrix.Evd();
            return eigen.EigenVectors;
        }

        private Vector<double> ProjectOntoEigenfaces(Vector<double> image)
        {
            return eigenVectors.Transpose() * image;
        }

        private string FindClosestMatch(Vector<double> projectedImage)
        {
            // Implement Euclidean distance to find the closest image
            return "Closest identity found";
        }
    }
}