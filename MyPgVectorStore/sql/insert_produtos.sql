INSERT INTO products (Title, Category, Summary, Description) VALUES 
-- 1 a 5: Gamers e Alta Performance
('Alienware m16 R2 - Intel Core Ultra 9, RTX 4070', 'Notebook gamer', 
 'Máquina de altíssimo desempenho para jogos em 4K, e-sports competitivos e realidade virtual. Excelente sistema de refrigeração e placa de vídeo dedicada de última geração.', 
 'Notebook projetado para entusiastas. Tela QHD de 240Hz, 32GB de RAM DDR5, 1TB SSD NVMe, teclado mecânico CherryMX e iluminação RGB por tecla.'),

('Acer Nitro 5 - Ryzen 7, RTX 3050', 'Notebook gamer custo-benefício', 
 'Notebook ideal para quem quer entrar no mundo dos jogos sem gastar muito. Roda jogos populares em qualidade média/alta e serve bem para estudos intensos.', 
 'Equilíbrio perfeito entre preço e performance. Tela Full HD 144Hz, 16GB de RAM, 512GB SSD e sistema de resfriamento dual-fan.'),

('ASUS ROG Zephyrus G14 - Ryzen 9, RTX 4060', 'Notebook para programação e Jogos', 
 'Poder de fogo de um desktop em um formato compacto e leve. Perfeito para desenvolvedores que programam de dia e jogam títulos pesados à noite.', 
 'Chassi em liga de magnésio, tela OLED 120Hz de 14 polegadas, 1.6kg, 32GB de RAM, bateria de longa duração e design premium com painel AniMe Matrix.'),

('Lenovo Legion Pro 5i - Intel i7, RTX 4060', 'Notebook gamer', 
 'Excelente qualidade de construção e refrigeração avançada para sessões prolongadas de jogos e streaming ao vivo sem queda de desempenho (thermal throttling).', 
 'Tela WQXGA de 16 polegadas 165Hz, 16GB RAM, 1TB SSD Gen4. Otimizado com IA para distribuir energia entre CPU e GPU de acordo com a necessidade do jogo.'),

('Avell A70 ION - Core i7, RTX 4070', 'Notebook para engenharia e Jogos', 
 'Estação de trabalho disfarçada de notebook gamer. Ideal para rodar softwares pesados de modelagem 3D, CAD, renderização arquitetônica e os jogos mais recentes.', 
 'Chassi discreto, tela 15.6" QHD, suporte a até 64GB de RAM, 2 slots M.2, conectividade Thunderbolt 4 e teclado retroiluminado.'),

-- 6 a 10: Programação, Criadores e Profissionais
('Apple MacBook Pro 16" - Chip M3 Max', 'Notebook para programação e edição', 
 'Desempenho monstruoso para compilação de código, desenvolvimento de apps (iOS/Android) e edição profissional de vídeos em 4K/8K. Bateria que dura o dia todo.', 
 'Tela Liquid Retina XDR de 16", chip M3 Max com CPU de 14 núcleos e GPU de 30 núcleos, 36GB de Memória Unificada, 1TB de SSD. O melhor do ecossistema Apple.'),

('Dell XPS 15 - Intel i9, RTX 4070', 'Notebook para criadores', 
 'Visual premium, bordas ultrafinas e tela com calibração de cor perfeita. Voltado para designers, fotógrafos, editores e profissionais exigentes.', 
 'Acabamento em fibra de carbono e alumínio usinado. Tela 15.6" OLED 3.5K touch, 32GB RAM DDR5, 1TB SSD. Placa de vídeo pronta para aceleração gráfica no pacote Adobe.'),

('Lenovo ThinkPad X1 Carbon - Intel i7', 'Notebook corporativo e programação', 
 'O padrão ouro para executivos e programadores. Teclado extremamente confortável, resistência militar, leveza incomparável e foco máximo em segurança e privacidade.', 
 'Pesando apenas 1.12kg. Tela 14" WUXGA de baixo consumo, 16GB RAM soldada, 512GB SSD, leitor de digitais integrado ao botão de energia e tampa de privacidade na webcam.'),

('Samsung Galaxy Book3 Ultra - Core i7, RTX 4050', 'Notebook para criadores e mobilidade', 
 'Poder gráfico com muita leveza. Integração perfeita com smartphones e tablets do ecossistema Samsung, ideal para quem produz conteúdo e precisa de portabilidade.', 
 'Tela AMOLED 3K de 16", 120Hz, 32GB RAM, 1TB SSD. Extremamente fino para uma máquina que possui placa de vídeo dedicada. Carregador compacto de 100W.'),

('Microsoft Surface Laptop 5 - Intel i7', 'Notebook executivo premium', 
 'Design minimalista, tela sensível ao toque no formato 3:2 (excelente para ler código e planilhas) e integração nativa perfeita com o ambiente Windows.', 
 'Tela PixelSense de 13.5", acabamento em Alcantara, 16GB RAM, 512GB SSD. Focado na experiência de uso fluida e design sofisticado para o ambiente de negócios.'),

-- 11 a 15: Mobilidade, Estudantes e Custo-Benefício
('Apple MacBook Air 13" - Chip M2', 'Notebook para estudantes e mobilidade', 
 'Totalmente silencioso (sem ventoinhas), absurdamente leve e com bateria para mais de 15 horas. A melhor opção para levar para a faculdade ou cafés.', 
 'Tela Liquid Retina de 13.6", chip M2, 8GB de Memória Unificada, 256GB SSD. Chassi unibody de alumínio de apenas 1.24kg e webcam 1080p.'),

('Dell Inspiron 15 3000 - Intel Core i5', 'Notebook básico para home office', 
 'Notebook honesto para o dia a dia. Perfeito para navegação na web, edição de documentos, planilhas, consumo de mídia e aulas online.', 
 'Tela Full HD de 15.6", processador Intel Core i5 de 12ª geração, 8GB de RAM (expansível), 512GB SSD. Design limpo e prático para o uso doméstico.'),

('Lenovo IdeaPad 3 - Ryzen 5', 'Notebook custo-benefício', 
 'Ótimo preço com bom desempenho geral. Seu processador permite um multitarefa fluido e até arriscar alguns jogos leves graças aos gráficos integrados Radeon.', 
 'Tela 15.6" antirreflexo, AMD Ryzen 5 série 5000, 8GB RAM, 256GB SSD. Possui teclado numérico, ideal para quem trabalha muito com cálculos e finanças.'),

('ASUS Zenbook 14 OLED - Intel Core i7', 'Notebook ultrafino premium', 
 'Experiência visual de cinema em um notebook fácil de carregar. Cores vibrantes e preto absoluto, ótimo para consumo de mídia e produtividade diária.', 
 'Tela OLED 2.8K de 90Hz, 16GB RAM, 1TB SSD, certificação Intel Evo (garante despertar rápido e bateria longa), chassi de alumínio com certificação militar.'),

('LG Gram 17 - Intel Core i7', 'Notebook ultrafino de tela grande', 
 'Um milagre da engenharia: possui uma enorme tela de 17 polegadas, mas pesa menos que a maioria dos notebooks de 13 polegadas. Focado em multitarefa extrema.', 
 'Pesando apenas 1.35kg. Tela IPS WQXGA de 17 polegadas (proporção 16:10), 16GB RAM, 1TB SSD, bateria de 80Wh para o dia inteiro. Ideal para trabalhar com várias janelas.'),

-- 16 a 20: Híbridos (2 em 1) e Versáteis
('HP Spectre x360 14 - Intel Core i7', 'Notebook 2 em 1 executivo', 
 'Design luxuoso com corte em diamante que se transforma em tablet. Perfeito para apresentações corporativas, reuniões de negócios e anotações com caneta digital.', 
 'Dobradiça 360 graus, tela OLED de 13.5 polegadas touch, 16GB RAM, 1TB SSD. Acompanha caneta HP recarregável e possui câmera de 5MP inteligente.'),

('Lenovo Yoga 7i - Intel Core i5', 'Notebook 2 em 1 criativo', 
 'Flexibilidade para estudar, desenhar ou assistir filmes em modo tenda. Boa performance para ilustradores amadores e estudantes criativos.', 
 'Tela de 14" WUXGA sensível ao toque, 16GB RAM, 512GB SSD. Áudio otimizado com Dolby Atmos e bordas arredondadas para maior conforto ao segurar como tablet.'),

('Acer Aspire 5 - Intel Core i5, MX550', 'Notebook para estudos e jogos leves', 
 'Voltado para quem precisa de um notebook de estudos ou escritório, mas quer uma placa de vídeo dedicada de entrada para jogar The Sims, Valorant ou CS:GO no fim de semana.', 
 'Tela 15.6" Full HD, processador i5 de 12ª geração, placa de vídeo Nvidia MX550 2GB, 8GB RAM, 512GB SSD e tampa em alumínio.'),

('Samsung Galaxy Book2 360 - Core i5', 'Notebook 2 em 1 custo-benefício', 
 'Tela Super AMOLED sensível ao toque num formato conversível por um preço acessível. Excelente para consumo de filmes e leitura de PDFs no modo tablet.', 
 'Tela 13.3" AMOLED, peso de apenas 1.16kg, 8GB RAM, 256GB SSD, Windows 11 Home. Fino, leve e totalmente integrado ao ecossistema Galaxy.'),

('Dell Latitude 5440 - Intel Core i5', 'Notebook corporativo', 
 'Foco em durabilidade e manutenção fácil para o setor de TI das empresas. Não possui visual chamativo, mas garante estabilidade e conectividade robusta.', 
 'Forte presença de portas (RJ45, USB-A, Thunderbolt), 16GB RAM, 256GB SSD. Construído com plásticos e fibras de carbono reciclados. Recursos avançados de gerenciamento vPro.');