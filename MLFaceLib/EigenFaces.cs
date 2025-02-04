using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace MLFaceLib
{
    public class EigenFaces
    {
        private List<Vector<double>> trainingImages = new List<Vector<double>>();
        private Vector<double>? meanImage;
        private Matrix<double>? eigenVectors;

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
            using (Image<Rgba32> image = Image.Load<Rgba32>(path))
            {
                double[] pixels = new double[image.Width * image.Height];
                for (int y = 0; y < image.Height; y++)
                {
                    // Get the pixel row memory and then obtain a span from it.
                    var rowMemory = image.Frames.RootFrame.DangerousGetPixelRowMemory(y);
                    var pixelRowSpan = rowMemory.Span;
                    
                    //Span<Rgba32> pixelRowSpan = image.GetPixelRowSpan(y);
                    for (int x = 0; x < image.Width; x++)
                    {
                        pixels[y * image.Width + x] = pixelRowSpan[x].R / 255.0;
                    }
                }

                return Vector<double>.Build.Dense(pixels);
            }
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
            if(eigenVectors == null)
                throw new InvalidOperationException("Train the model first");
            
            return eigenVectors.Transpose() * image;
        }

        private string FindClosestMatch(Vector<double> projectedImage)
        {
            // Implement Euclidean distance to find the closest image
            return "Closest identity found";
        }
    }
}