namespace MLFaceLib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


   /// <summary>
/// A simple static helper class for basic matrix operations.
/// </summary>
public static class MatrixOperations
{
    // Multiply two matrices: result = A (m x n) * B (n x p)  -> (m x p)
    public static double[,] Multiply(double[,] A, double[,] B)
    {
        int m = A.GetLength(0);
        int n = A.GetLength(1);
        int p = B.GetLength(1);
        double[,] result = new double[m, p];
        for (int i = 0; i < m; i++)
            for (int j = 0; j < p; j++)
            {
                double sum = 0;
                for (int k = 0; k < n; k++)
                    sum += A[i, k] * B[k, j];
                result[i, j] = sum;
            }
        return result;
    }

    // Multiply matrix (m x n) by vector (n) -> vector (m)
    public static double[] Multiply(double[,] matrix, double[] vector)
    {
        int m = matrix.GetLength(0);
        int n = matrix.GetLength(1);
        if (vector.Length != n)
            throw new ArgumentException("Matrix columns and vector length do not match.");
        double[] result = new double[m];
        for (int i = 0; i < m; i++)
        {
            double sum = 0;
            for (int j = 0; j < n; j++)
                sum += matrix[i, j] * vector[j];
            result[i] = sum;
        }
        return result;
    }

    // Transpose a matrix.
    public static double[,] Transpose(double[,] A)
    {
        int m = A.GetLength(0);
        int n = A.GetLength(1);
        double[,] T = new double[n, m];
        for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                T[j, i] = A[i, j];
        return T;
    }

    // Create an identity matrix of size n.
    public static double[,] Identity(int n)
    {
        double[,] I = new double[n, n];
        for (int i = 0; i < n; i++)
            I[i, i] = 1.0;
        return I;
    }

    // Compute the Euclidean norm of a vector.
    public static double Norm(double[] v)
    {
        double sum = 0;
        foreach (var d in v)
            sum += d * d;
        return Math.Sqrt(sum);
    }

    // Normalize a vector (in place).
    public static void Normalize(double[] v)
    {
        double norm = Norm(v);
        if (norm > 1e-10)
        {
            for (int i = 0; i < v.Length; i++)
                v[i] /= norm;
        }
    }

    // Invert a square matrix using Gaussian elimination.
    public static double[,] Inverse(double[,] A)
    {
        int n = A.GetLength(0);
        if (n != A.GetLength(1))
            throw new ArgumentException("Matrix must be square.");

        double[,] result = Identity(n);
        double[,] temp = (double[,])A.Clone();

        for (int i = 0; i < n; i++)
        {
            // Find pivot
            double pivot = temp[i, i];
            if (Math.Abs(pivot) < 1e-10)
                throw new Exception("Matrix is singular or nearly singular.");

            // Scale row i
            for (int j = 0; j < n; j++)
            {
                temp[i, j] /= pivot;
                result[i, j] /= pivot;
            }

            // Eliminate column i in other rows
            for (int k = 0; k < n; k++)
            {
                if (k == i)
                    continue;
                double factor = temp[k, i];
                for (int j = 0; j < n; j++)
                {
                    temp[k, j] -= factor * temp[i, j];
                    result[k, j] -= factor * result[i, j];
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Computes the eigen–values and eigen–vectors of a real symmetric matrix using the Jacobi method.
    /// The matrix A is modified during the process.
    /// </summary>
    public static void JacobiEigen(double[,] A, out double[] eigenvalues, out double[,] eigenvectors)
    {
        int n = A.GetLength(0);
        eigenvalues = new double[n];
        eigenvectors = Identity(n);
        const int maxIter = 100 * 100;
        const double eps = 1e-10;

        for (int iter = 0; iter < maxIter; iter++)
        {
            // Find the largest off-diagonal absolute value in A.
            double max = 0;
            int p = 0, q = 0;
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    double absAij = Math.Abs(A[i, j]);
                    if (absAij > max)
                    {
                        max = absAij;
                        p = i;
                        q = j;
                    }
                }
            if (max < eps)
                break; // Converged

            // Compute Jacobi rotation.
            double theta = 0.5 * (A[q, q] - A[p, p]) / A[p, q];
            double t = Math.Sign(theta) / (Math.Abs(theta) + Math.Sqrt(1 + theta * theta));
            double c = 1.0 / Math.Sqrt(1 + t * t);
            double s = t * c;
            double tau = s / (1 + c);

            double app = A[p, p];
            double aqq = A[q, q];
            A[p, p] = app - t * A[p, q];
            A[q, q] = aqq + t * A[p, q];
            A[p, q] = 0;
            A[q, p] = 0;

            // Rotate other entries.
            for (int i = 0; i < n; i++)
            {
                if (i != p && i != q)
                {
                    double aip = A[i, p];
                    double aiq = A[i, q];
                    A[i, p] = aip - s * (aiq + tau * aip);
                    A[p, i] = A[i, p];
                    A[i, q] = aiq + s * (aip - tau * aiq);
                    A[q, i] = A[i, q];
                }
            }
            // Update eigenvectors.
            for (int i = 0; i < n; i++)
            {
                double vip = eigenvectors[i, p];
                double viq = eigenvectors[i, q];
                eigenvectors[i, p] = vip - s * (viq + tau * vip);
                eigenvectors[i, q] = viq + s * (vip - tau * viq);
            }
        }
        // The diagonal of A now holds the eigenvalues.
        for (int i = 0; i < n; i++)
            eigenvalues[i] = A[i, i];
    }

    // Multiply two matrices with proper dimension check (wrapper for clarity).
    public static double[,] MultiplyMatrices(double[,] A, double[,] B)
    {
        return Multiply(A, B);
    }
}

/// <summary>
/// A simple Fisherfaces–based face recognizer.
/// 
/// The recognizer is trained on a set of flattened image vectors (each a double[] of pixel values)
/// and associated integer labels. During training, it computes a PCA subspace to reduce the high–dimensional
/// image space and then computes an LDA projection (the Fisherfaces) to maximize between–class variance.
/// For recognition, a new image is projected into the Fisherface space and matched (by Euclidean distance)
/// against the training projections.
/// 
/// This implementation is for demonstration only.
/// </summary>
public class FisherFaceRecognizer
{
    // Fields for internal data.
    private int imageSize; // Length of each flattened image.
    private double[] mean; // Mean image (flattened).
    private double[,] pcaEigenvectors; // PCA eigenvectors (each column is a basis vector).
    private double[,] fisherfaces;     // Final Fisherface projection matrix.
    private double[,] trainingProjections; // Projections of training images into Fisherface space.
    private int[] trainingLabels;      // Training labels.
    private int numComponentsPCA;
    private int numComponentsLDA;

    /// <summary>
    /// Initializes a new instance.
    /// numComponentsPCA: number of PCA eigenvectors to keep.
    /// numComponentsLDA: number of LDA directions (usually <= number of classes – 1).
    /// </summary>
    public FisherFaceRecognizer(int numComponentsPCA, int numComponentsLDA)
    {
        this.numComponentsPCA = numComponentsPCA;
        this.numComponentsLDA = numComponentsLDA;
    }

    /// <summary>
    /// Train the recognizer given an array of flattened images and their corresponding labels.
    /// </summary>
    public void Train(double[][] images, int[] labels)
    {
        if (images == null || images.Length == 0)
            throw new ArgumentException("No images provided.");
        if (images.Length != labels.Length)
            throw new ArgumentException("The number of images and labels must match.");

        int numImages = images.Length;
        imageSize = images[0].Length;
        // Check that all images have the same size.
        for (int i = 0; i < numImages; i++)
        {
            if (images[i].Length != imageSize)
                throw new ArgumentException("All images must have the same size.");
        }

        // 1. Compute the mean image.
        mean = new double[imageSize];
        for (int i = 0; i < imageSize; i++)
        {
            double sum = 0;
            for (int j = 0; j < numImages; j++)
                sum += images[j][i];
            mean[i] = sum / numImages;
        }

        // 2. Subtract the mean from each image.
        double[][] centeredImages = new double[numImages][];
        for (int j = 0; j < numImages; j++)
        {
            centeredImages[j] = new double[imageSize];
            for (int i = 0; i < imageSize; i++)
                centeredImages[j][i] = images[j][i] - mean[i];
        }

        // 3. Build data matrix A where each column is a centered image (size: imageSize x numImages)
        double[,] A = new double[imageSize, numImages];
        for (int j = 0; j < numImages; j++)
            for (int i = 0; i < imageSize; i++)
                A[i, j] = centeredImages[j][i];

        // 4. Compute PCA via the “small–matrix” trick.
        // Compute L = A^T * A (size: numImages x numImages)
        double[,] L = new double[numImages, numImages];
        for (int i = 0; i < numImages; i++)
            for (int j = 0; j < numImages; j++)
            {
                double sum = 0;
                for (int k = 0; k < imageSize; k++)
                    sum += A[k, i] * A[k, j];
                L[i, j] = sum;
            }

        // Compute eigen–decomposition of L.
        MatrixOperations.JacobiEigen(L, out double[] eigenValues, out double[,] eigenVectorsL);

        // Sort eigenvalues (and corresponding eigenvectors) in descending order.
        var indices = Enumerable.Range(0, eigenValues.Length)
                                .OrderByDescending(i => eigenValues[i])
                                .ToArray();

        // 5. Compute PCA eigenvectors for the original covariance matrix.
        pcaEigenvectors = new double[imageSize, numComponentsPCA];
        for (int comp = 0; comp < numComponentsPCA; comp++)
        {
            int idx = indices[comp];
            // Get eigenvector from L (length = numImages)
            double[] v = new double[numImages];
            for (int j = 0; j < numImages; j++)
                v[j] = eigenVectorsL[j, idx];

            // Compute u = A * v (length = imageSize)
            double[] u = new double[imageSize];
            for (int i = 0; i < imageSize; i++)
            {
                double sum = 0;
                for (int j = 0; j < numImages; j++)
                    sum += A[i, j] * v[j];
                u[i] = sum;
            }
            // Normalize u.
            MatrixOperations.Normalize(u);
            // Set as the comp-th column of pcaEigenvectors.
            for (int i = 0; i < imageSize; i++)
                pcaEigenvectors[i, comp] = u[i];
        }

        // 6. Project training images into PCA subspace.
        // Let Y = (numComponentsPCA x numImages) where each column is y = pcaEigenvectors^T * (image - mean)
        double[,] pcaProjections = new double[numComponentsPCA, numImages];
        for (int j = 0; j < numImages; j++)
        {
            // For image j, get centered image vector.
            double[] x = centeredImages[j];
            for (int comp = 0; comp < numComponentsPCA; comp++)
            {
                double sum = 0;
                for (int i = 0; i < imageSize; i++)
                    sum += pcaEigenvectors[i, comp] * x[i];
                pcaProjections[comp, j] = sum;
            }
        }

        // 7. Compute LDA in the PCA space.
        // First, compute overall mean in PCA space.
        double[] overallMean = new double[numComponentsPCA];
        for (int comp = 0; comp < numComponentsPCA; comp++)
        {
            double sum = 0;
            for (int j = 0; j < numImages; j++)
                sum += pcaProjections[comp, j];
            overallMean[comp] = sum / numImages;
        }

        // Determine the unique classes.
        var classes = labels.Distinct().OrderBy(l => l).ToArray();
        int numClasses = classes.Length;

        // Initialize scatter matrices (size: numComponentsPCA x numComponentsPCA).
        double[,] S_w = new double[numComponentsPCA, numComponentsPCA];
        double[,] S_b = new double[numComponentsPCA, numComponentsPCA];

        // For each class, compute class mean and update S_w and S_b.
        for (int ci = 0; ci < numClasses; ci++)
        {
            int cls = classes[ci];
            // Find indices for this class.
            List<int> idxs = new List<int>();
            for (int j = 0; j < numImages; j++)
                if (labels[j] == cls)
                    idxs.Add(j);
            int n_c = idxs.Count;
            if (n_c == 0)
                continue;

            // Compute class mean in PCA space.
            double[] classMean = new double[numComponentsPCA];
            foreach (int j in idxs)
            {
                for (int comp = 0; comp < numComponentsPCA; comp++)
                    classMean[comp] += pcaProjections[comp, j];
            }
            for (int comp = 0; comp < numComponentsPCA; comp++)
                classMean[comp] /= n_c;

            // Update within-class scatter S_w.
            foreach (int j in idxs)
            {
                double[] diff = new double[numComponentsPCA];
                for (int comp = 0; comp < numComponentsPCA; comp++)
                    diff[comp] = pcaProjections[comp, j] - classMean[comp];
                // Outer product diff * diff^T
                for (int i = 0; i < numComponentsPCA; i++)
                    for (int j2 = 0; j2 < numComponentsPCA; j2++)
                        S_w[i, j2] += diff[i] * diff[j2];
            }

            // Update between-class scatter S_b.
            double[] meanDiff = new double[numComponentsPCA];
            for (int comp = 0; comp < numComponentsPCA; comp++)
                meanDiff[comp] = classMean[comp] - overallMean[comp];
            for (int i = 0; i < numComponentsPCA; i++)
                for (int j2 = 0; j2 < numComponentsPCA; j2++)
                    S_b[i, j2] += n_c * meanDiff[i] * meanDiff[j2];
        }

        // 8. Solve the generalized eigen–problem: S_b * v = lambda * S_w * v.
        // One common method is to compute S_w^-1/2 and form M = S_w^-1/2 * S_b * S_w^-1/2.
        // Then the eigenvectors of M give the LDA directions.
        // First compute eigen–decomposition of S_w.
        double[,] S_w_copy = (double[,])S_w.Clone(); // because JacobiEigen modifies its input.
        MatrixOperations.JacobiEigen(S_w_copy, out double[] eigValsSw, out double[,] eigVecsSw);
        // Form D_inv_sqrt.
        double[,] D_inv_sqrt = new double[numComponentsPCA, numComponentsPCA];
        for (int i = 0; i < numComponentsPCA; i++)
        {
            double d = eigValsSw[i];
            double invSqrt = (Math.Abs(d) > 1e-10) ? 1.0 / Math.Sqrt(d) : 0;
            D_inv_sqrt[i, i] = invSqrt;
        }
        // Compute S_w^-1/2 = U * D_inv_sqrt * U^T.
        double[,] U = eigVecsSw;
        double[,] Ut = MatrixOperations.Transpose(U);
        double[,] tempMat = MatrixOperations.Multiply(U, D_inv_sqrt);
        double[,] S_w_inv_sqrt = MatrixOperations.Multiply(tempMat, Ut);

        // Form M = S_w_inv_sqrt * S_b * S_w_inv_sqrt.
        double[,] tempM = MatrixOperations.Multiply(S_w_inv_sqrt, S_b);
        double[,] Mmat = MatrixOperations.Multiply(tempM, S_w_inv_sqrt);

        // Compute eigen–decomposition of Mmat.
        MatrixOperations.JacobiEigen(Mmat, out double[] eigValsM, out double[,] eigVecsM);

        // Sort eigenvalues and take top numComponentsLDA eigenvectors.
        var ldaIndices = Enumerable.Range(0, eigValsM.Length)
                                   .OrderByDescending(i => eigValsM[i])
                                   .Take(numComponentsLDA)
                                   .ToArray();

        // Form LDA eigenvectors in PCA space: u = S_w_inv_sqrt * (selected eigenvector of M)
        double[,] ldaEigenvectors = new double[numComponentsPCA, numComponentsLDA];
        for (int col = 0; col < numComponentsLDA; col++)
        {
            int idx = ldaIndices[col];
            // Get the idx–th eigenvector of M (length = numComponentsPCA)
            double[] v = new double[numComponentsPCA];
            for (int i = 0; i < numComponentsPCA; i++)
                v[i] = eigVecsM[i, idx];
            // Multiply: u = S_w_inv_sqrt * v.
            double[] uVec = new double[numComponentsPCA];
            for (int i = 0; i < numComponentsPCA; i++)
            {
                double sum = 0;
                for (int j = 0; j < numComponentsPCA; j++)
                    sum += S_w_inv_sqrt[i, j] * v[j];
                uVec[i] = sum;
            }
            // Normalize uVec.
            MatrixOperations.Normalize(uVec);
            // Set as the col–th column.
            for (int i = 0; i < numComponentsPCA; i++)
                ldaEigenvectors[i, col] = uVec[i];
        }

        // 9. The final Fisherface projection matrix in the original image space is:
        // fisherfaces = pcaEigenvectors * ldaEigenvectors.
        fisherfaces = MatrixOperations.Multiply(pcaEigenvectors, ldaEigenvectors);

        // 10. Project each training image (after mean subtraction) into Fisherface space.
        trainingProjections = new double[numComponentsLDA, numImages];
        for (int j = 0; j < numImages; j++)
        {
            // Get the centered image.
            double[] x = centeredImages[j];
            // Compute projection: f = fisherfaces^T * x.
            for (int comp = 0; comp < numComponentsLDA; comp++)
            {
                double sum = 0;
                for (int i = 0; i < imageSize; i++)
                    sum += fisherfaces[i, comp] * x[i];
                trainingProjections[comp, j] = sum;
            }
        }

        // Save training labels.
        trainingLabels = (int[])labels.Clone();
    }

    /// <summary>
    /// Recognize the face in the given flattened image by projecting it into the Fisherface space
    /// and returning the label of the closest training sample.
    /// </summary>
    public int Recognize(double[] image)
    {
        if (image.Length != imageSize)
            throw new ArgumentException("Image size does not match training images.");

        // Subtract mean.
        double[] x = new double[imageSize];
        for (int i = 0; i < imageSize; i++)
            x[i] = image[i] - mean[i];

        // Project into Fisherface space.
        double[] projection = new double[numComponentsLDA];
        for (int comp = 0; comp < numComponentsLDA; comp++)
        {
            double sum = 0;
            for (int i = 0; i < imageSize; i++)
                sum += fisherfaces[i, comp] * x[i];
            projection[comp] = sum;
        }

        // Find the nearest training projection (using Euclidean distance).
        int bestIndex = -1;
        double minDist = double.MaxValue;
        int numTrain = trainingProjections.GetLength(1);
        for (int j = 0; j < numTrain; j++)
        {
            double dist = 0;
            for (int comp = 0; comp < numComponentsLDA; comp++)
            {
                double diff = projection[comp] - trainingProjections[comp, j];
                dist += diff * diff;
            }
            if (dist < minDist)
            {
                minDist = dist;
                bestIndex = j;
            }
        }
        return trainingLabels[bestIndex];
    }

    /// <summary>
    /// Saves the model (mean vector, Fisherfaces matrix, and training projections/labels) to a text file.
    /// </summary>
    public void SaveModel(string filename)
    {
        using (StreamWriter writer = new StreamWriter(filename))
        {
            writer.WriteLine(imageSize);
            writer.WriteLine(numComponentsPCA);
            writer.WriteLine(numComponentsLDA);
            writer.WriteLine(trainingProjections.GetLength(1)); // number of training samples

            // Write mean vector.
            writer.WriteLine(string.Join(" ", mean));

            // Write fisherfaces matrix (row by row).
            for (int i = 0; i < imageSize; i++)
            {
                double[] row = new double[numComponentsLDA];
                for (int j = 0; j < numComponentsLDA; j++)
                    row[j] = fisherfaces[i, j];
                writer.WriteLine(string.Join(" ", row));
            }

            // Write training projections (each sample in one line).
            int numTrain = trainingProjections.GetLength(1);
            for (int j = 0; j < numTrain; j++)
            {
                double[] proj = new double[numComponentsLDA];
                for (int comp = 0; comp < numComponentsLDA; comp++)
                    proj[comp] = trainingProjections[comp, j];
                writer.WriteLine(string.Join(" ", proj));
            }

            // Write training labels.
            writer.WriteLine(string.Join(" ", trainingLabels));
        }
    }

    /// <summary>
    /// Loads the model from a text file saved with SaveModel.
    /// </summary>
    public void LoadModel(string filename)
    {
        using (StreamReader reader = new StreamReader(filename))
        {
            imageSize = int.Parse(reader.ReadLine());
            numComponentsPCA = int.Parse(reader.ReadLine());
            numComponentsLDA = int.Parse(reader.ReadLine());
            int numTrain = int.Parse(reader.ReadLine());

            mean = reader.ReadLine().Split(' ').Select(double.Parse).ToArray();

            // Read fisherfaces matrix.
            fisherfaces = new double[imageSize, numComponentsLDA];
            for (int i = 0; i < imageSize; i++)
            {
                double[] row = reader.ReadLine().Split(' ').Select(double.Parse).ToArray();
                for (int j = 0; j < numComponentsLDA; j++)
                    fisherfaces[i, j] = row[j];
            }

            // Read training projections.
            trainingProjections = new double[numComponentsLDA, numTrain];
            for (int j = 0; j < numTrain; j++)
            {
                double[] proj = reader.ReadLine().Split(' ').Select(double.Parse).ToArray();
                for (int comp = 0; comp < numComponentsLDA; comp++)
                    trainingProjections[comp, j] = proj[comp];
            }

            // Read training labels.
            trainingLabels = reader.ReadLine().Split(' ').Select(int.Parse).ToArray();
        }
    }
}

