using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Advanced;


namespace MLFaceLib;

 /// <summary>
    /// Exemplo simplificado de um detector de objetos baseado em Haar Cascade usando SixLabors.ImageSharp.
    /// </summary>
    public class HaarObjectDetector
    {
        // Lista de estágios que compõem a cascata.
        private List<HaarStage> _stages;

        /// <summary>
        /// Construtor. Nesta versão de demonstração a cascata é construída com parâmetros fixos.
        /// </summary>
        public HaarObjectDetector()
        {
            // Para demonstração, assumimos que a janela de detecção tem tamanho 24x24 pixels.
            // Criamos um estágio simples com um único Haar feature do tipo "two-rectangle horizontal".
            HaarFeature demoFeature = new HaarFeature(
                featureType: HaarFeatureType.TwoRectangleHorizontal,
                featureRect: new Rectangle(0, 0, 24, 24),  // Coordenadas normalizadas para janela base de 24x24
                weight: 1.0,
                threshold: 1000   // Limiar arbitrário para demonstração
            );

            // O estágio é aprovado se a soma ponderada das features ultrapassar o threshold do estágio.
            HaarStage demoStage = new HaarStage(
                features: new List<HaarFeature> { demoFeature },
                stageThreshold: 0.5   // Valor arbitrário
            );

            _stages = new List<HaarStage> { demoStage };
        }

        /// <summary>
        /// Realiza a detecção de objetos na imagem colorida, utilizando varredura com múltiplas escalas.
        /// </summary>
        /// <param name="image">Imagem de entrada (colorida) do tipo Image&lt;Rgba32&gt;.</param>
        /// <param name="scaleFactor">Fator de escala para aumentar a janela (padrão: 1.1).</param>
        /// <param name="minSize">Tamanho mínimo da detecção (padrão: 24x24).</param>
        /// <returns>Lista de retângulos indicando onde os objetos foram detectados.</returns>
        public List<Rectangle> DetectObjects(Image image, double scaleFactor = 1.1, Size? minSize = null)
        {
            List<Rectangle> detections = new List<Rectangle>();

            // Converte a imagem para tons de cinza.
            using Image<L8> grayImage = ConvertToGrayscale(image);

            // Calcula a imagem integral para acesso rápido às somas de pixel.
            long[,] integralImage = ComputeIntegralImage(grayImage);

            // Tamanho base da janela (neste exemplo, 24x24)
            int baseWidth = 24;
            int baseHeight = 24;
            Size minDetectionSize = minSize ?? new Size(baseWidth, baseHeight);

            // Variáveis para escala e tamanho da janela atual
            double currentScale = 1.0;
            int windowWidth = baseWidth;
            int windowHeight = baseHeight;

            // Varre enquanto a janela escalada couber na imagem
            while (windowWidth <= image.Width && windowHeight <= image.Height)
            {
                // Passo fixo para a varredura (poderia ser ajustado proporcionalmente à escala)
                int step = 4;

                for (int y = 0; y <= image.Height - windowHeight; y += step)
                {
                    for (int x = 0; x <= image.Width - windowWidth; x += step)
                    {
                        // Se a janela passar por todos os estágios da cascata, considera-se uma detecção.
                        if (PassesCascade(integralImage, x, y, windowWidth, windowHeight))
                        {
                            detections.Add(new Rectangle(x, y, windowWidth, windowHeight));
                        }
                    }
                }

                // Atualiza a escala e o tamanho da janela para a próxima iteração.
                currentScale *= scaleFactor;
                windowWidth = (int)(baseWidth * currentScale);
                windowHeight = (int)(baseHeight * currentScale);
            }

            return detections;
        }

        /// <summary>
        /// Verifica se a janela definida por (windowX, windowY, windowWidth, windowHeight) passa por todos os estágios da cascata.
        /// </summary>
        private bool PassesCascade(long[,] integralImage, int windowX, int windowY, int windowWidth, int windowHeight)
        {
            // Para cada estágio, computa a resposta das features.
            foreach (var stage in _stages)
            {
                double stageSum = 0;
                foreach (var feature in stage.Features)
                {
                    double featureValue = feature.ComputeFeature(integralImage, windowX, windowY, windowWidth, windowHeight);
                    // Se a feature não atingir o limiar, a janela é descartada.
                    if (featureValue < feature.Threshold)
                    {
                        return false;
                    }
                    else
                    {
                        stageSum += featureValue * feature.Weight;
                    }
                }

                // Se a soma ponderada das features não atingir o threshold do estágio, a janela é rejeitada.
                if (stageSum < stage.StageThreshold)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Converte uma imagem colorida (Image&lt;Rgba32&gt;) para tons de cinza (Image&lt;L8&gt;).
        /// </summary>
        private Image<L8> ConvertToGrayscale(Image original)
        {
            // O método CloneAs converte a imagem para o pixel format desejado (neste caso, L8 – 8 bits em tons de cinza).
            return original.CloneAs<L8>();
        }

        /// <summary>
        /// Calcula a imagem integral de uma imagem em tons de cinza.
        /// Cada posição [x, y] contém a soma de todos os pixels da região (0,0) até (x,y).
        /// </summary>
        private long[,] ComputeIntegralImage(Image<L8> grayImage)
        {
            int width = grayImage.Width;
            int height = grayImage.Height;
            long[,] integral = new long[width, height];

            for (int y = 0; y < height; y++)
            {
                long rowSum = 0;
                // Get the pixel row memory and then obtain a span from it.
                var rowMemory = grayImage.Frames.RootFrame.DangerousGetPixelRowMemory(y);
                var rowSpan = rowMemory.Span;

                for (int x = 0; x < width; x++)
                {
                    // Since the image is in L8 format, each pixel is of type L8.
                    byte pixelValue = rowSpan[x].PackedValue;
                    rowSum += pixelValue;

                    if (y == 0)
                        integral[x, y] = rowSum;
                    else
                        integral[x, y] = integral[x, y - 1] + rowSum;
                }
            }

            return integral;
        }

        /// <summary>
        /// Retorna a soma dos valores de pixel em um retângulo, utilizando a imagem integral.
        /// </summary>
        public static long SumRectangle(long[,] integral, int x, int y, int width, int height)
        {
            int x2 = x + width - 1;
            int y2 = y + height - 1;

            long A = (x > 0 && y > 0) ? integral[x - 1, y - 1] : 0;
            long B = (y > 0) ? integral[x2, y - 1] : 0;
            long C = (x > 0) ? integral[x - 1, y2] : 0;
            long D = integral[x2, y2];

            return D - B - C + A;
        }
    }

    #region Classes de Apoio

    /// <summary>
    /// Tipos de features (neste exemplo, implementamos duas variações simples).
    /// </summary>
    public enum HaarFeatureType
    {
        TwoRectangleHorizontal,
        TwoRectangleVertical
        // Outros tipos (ex.: three-rectangle, four-rectangle) podem ser adicionados.
    }

    /// <summary>
    /// Representa uma Haar Feature. Nesta implementação simplificada a feature é definida
    /// em um sistema de coordenadas normalizado (por exemplo, para uma janela 24x24) e é escalada conforme a janela de detecção.
    /// </summary>
    public class HaarFeature
    {
        public HaarFeatureType FeatureType { get; private set; }
        public Rectangle FeatureRect { get; private set; }
        public double Weight { get; private set; }
        public double Threshold { get; private set; }

        public HaarFeature(HaarFeatureType featureType, Rectangle featureRect, double weight, double threshold)
        {
            FeatureType = featureType;
            FeatureRect = featureRect;
            Weight = weight;
            Threshold = threshold;
        }

        /// <summary>
        /// Calcula o valor da feature para uma janela de detecção definida pela posição (windowX, windowY) e tamanho (windowWidth, windowHeight).
        /// A feature é escalada de acordo com o tamanho da janela (assumindo que FeatureRect foi definida para uma janela base de 24x24).
        /// </summary>
        public double ComputeFeature(long[,] integral, int windowX, int windowY, int windowWidth, int windowHeight)
        {
            // Fatores de escala para ajustar a feature à janela atual.
            double scaleX = (double)windowWidth / 24.0;
            double scaleY = (double)windowHeight / 24.0;

            // Escala o retângulo da feature para a janela atual.
            Rectangle scaledRect = new Rectangle(
                windowX + (int)(FeatureRect.X * scaleX),
                windowY + (int)(FeatureRect.Y * scaleY),
                (int)(FeatureRect.Width * scaleX),
                (int)(FeatureRect.Height * scaleY)
            );

            double featureValue = 0;

            switch (FeatureType)
            {
                case HaarFeatureType.TwoRectangleHorizontal:
                    // Divide o retângulo em duas partes verticais iguais.
                    int halfWidth = scaledRect.Width / 2;
                    Rectangle leftRect = new Rectangle(scaledRect.X, scaledRect.Y, halfWidth, scaledRect.Height);
                    Rectangle rightRect = new Rectangle(scaledRect.X + halfWidth, scaledRect.Y, scaledRect.Width - halfWidth, scaledRect.Height);

                    long sumLeft = HaarObjectDetector.SumRectangle(integral, leftRect.X, leftRect.Y, leftRect.Width, leftRect.Height);
                    long sumRight = HaarObjectDetector.SumRectangle(integral, rightRect.X, rightRect.Y, rightRect.Width, rightRect.Height);

                    featureValue = sumLeft - sumRight;
                    break;

                case HaarFeatureType.TwoRectangleVertical:
                    // Divide o retângulo em duas partes horizontais iguais.
                    int halfHeight = scaledRect.Height / 2;
                    Rectangle topRect = new Rectangle(scaledRect.X, scaledRect.Y, scaledRect.Width, halfHeight);
                    Rectangle bottomRect = new Rectangle(scaledRect.X, scaledRect.Y + halfHeight, scaledRect.Width, scaledRect.Height - halfHeight);

                    long sumTop = HaarObjectDetector.SumRectangle(integral, topRect.X, topRect.Y, topRect.Width, topRect.Height);
                    long sumBottom = HaarObjectDetector.SumRectangle(integral, bottomRect.X, bottomRect.Y, bottomRect.Width, bottomRect.Height);

                    featureValue = sumTop - sumBottom;
                    break;

                default:
                    throw new NotImplementedException("Feature type not implemented.");
            }

            return featureValue;
        }
    }

    /// <summary>
    /// Representa um estágio da cascata. Cada estágio contém uma lista de Haar Features e um threshold.
    /// Se uma janela não passar em algum estágio, ela é imediatamente descartada.
    /// </summary>
    public class HaarStage
    {
        public List<HaarFeature> Features { get; private set; }
        public double StageThreshold { get; private set; }

        public HaarStage(List<HaarFeature> features, double stageThreshold)
        {
            Features = features;
            StageThreshold = stageThreshold;
        }
    }

    #endregion