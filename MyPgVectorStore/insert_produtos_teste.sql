-- Massa de dados adicional para testes de busca semântica (não substitui insert_produtos.sql).
-- Cobre categorias que ainda não existiam na base original, pra estressar melhor a diferenciação
-- entre embeddings (título + categoria + resumo + descrição).

INSERT INTO products (Title, Category, Summary, Description) VALUES

-- 1 a 10: Gamers (entrada a premium, incluindo AMD Advantage e ultraportátil gamer)
('Acer Aspire 7 - Ryzen 5, GTX 1650', 'Notebook gamer de entrada',
 'Primeiro notebook gamer para quem está começando. Roda jogos leves e moderados em configurações médias sem gastar muito.',
 'Tela 15.6" Full HD 60Hz, 8GB RAM (expansível), 512GB SSD, placa de vídeo dedicada de entrada. Bom para League of Legends, Valorant e Fortnite.'),

('ASUS TUF Gaming A15 - Ryzen 7, RTX 4050', 'Notebook gamer AMD Advantage',
 'Combinação certificada AMD para máxima eficiência energética em jogos, com ótima autonomia de bateria mesmo em uso intenso.',
 'Chassi com certificação militar MIL-STD-810H, tela 15.6" 144Hz, 16GB RAM, 512GB SSD, teclado com iluminação RGB de 4 zonas.'),

('MSI Katana 15 - Intel i7, RTX 4060', 'Notebook gamer intermediário',
 'Equilíbrio entre desempenho gráfico e preço, com visual mais discreto que os gamers tradicionais, podendo ser usado no escritório.',
 'Tela 15.6" Full HD 144Hz, 16GB RAM DDR5, 1TB SSD NVMe, sistema Cooler Boost 5 com 2 ventoinhas e 6 tubos de calor.'),

('Razer Blade 14 - Ryzen 9, RTX 4070', 'Notebook gamer ultraportátil premium',
 'Notebook gamer no formato mais compacto do mercado, com acabamento em alumínio CNC digno de ultrabook executivo.',
 'Chassi unibody de 14", tela QHD+ 240Hz, 32GB RAM, 1TB SSD, teclado por membrana individual RGB Chroma.'),

('Gigabyte AORUS 17X - Core i9, RTX 4090', 'Notebook gamer desktop replacement',
 'Substituto de desktop para quem quer o máximo de desempenho possível em um notebook, sem economizar em nada.',
 'Tela 17.3" QHD 240Hz, 64GB RAM, 2TB SSD RAID 0, teclado mecânico opto-mecânico com switches Omron, sistema de refrigeração WINDFORCE Infinity.'),

('Lenovo LOQ 15 - Core i5, RTX 3050', 'Notebook gamer custo-benefício',
 'Porta de entrada acessível para o ecossistema Legion, com desempenho suficiente para a maioria dos jogos competitivos atuais.',
 'Tela 15.6" Full HD 144Hz, 8GB RAM, 512GB SSD, modo de desempenho ajustável via Lenovo Vantage.'),

('HP Omen 16 - Ryzen 7, RTX 4060', 'Notebook gamer com foco em streaming',
 'Construído pensando em criadores de conteúdo gamer, com câmera e microfone de qualidade superior para lives e gravações.',
 'Tela 16.1" QHD 165Hz, 16GB RAM, 1TB SSD, webcam 5MP com IA de enquadramento automático, software OMEN Gaming Hub.'),

('Dell G15 - Core i7, RTX 4050', 'Notebook gamer robusto',
 'Notebook gamer da Dell com boa relação de refrigeração e durabilidade, recomendado para sessões longas de jogo em casa.',
 'Tela 15.6" Full HD 120Hz, 16GB RAM, 512GB SSD, sistema térmico Dual Fan com quad-vent, Alienware Command Center integrado.'),

('Acer Predator Helios Neo 16 - Core i9, RTX 4070', 'Notebook gamer profissional',
 'Voltado para jogadores competitivos e criadores que exigem alto refresh rate e cores precisas na mesma máquina.',
 'Tela Mini LED 16" WQXGA 250Hz, 32GB RAM, 1TB SSD, teclado PredatorSense RGB por tecla, tecnologia PredatorSense de overclock.'),

('Samsung Odyssey - Core i7, RTX 4060', 'Notebook gamer com tela AMOLED',
 'Um dos poucos notebooks gamer com tela AMOLED, entregando cores extremamente vivas para jogos e conteúdo HDR.',
 'Tela 16" AMOLED 2.5K 165Hz, 16GB RAM, 512GB SSD, alto-falantes AKG com Dolby Atmos.'),

-- 11 a 20: Programação & Devs (Linux, backend, mobile, DevOps, embarcados)
('Framework Laptop 13 - Core i7, Linux', 'Notebook para desenvolvedores Linux',
 'Notebook modular com peças substituíveis e portas intercambiáveis, muito popular entre desenvolvedores que valorizam reparabilidade e open source.',
 'Tela 13.5" 2256x1504, 32GB RAM, 1TB SSD NVMe, portas Expansion Card customizáveis (USB-C, HDMI, USB-A, SD), compatível com Ubuntu e Fedora.'),

('System76 Lemur Pro - Core i7, Linux', 'Notebook para desenvolvimento backend',
 'Feito sob medida para quem programa em Linux o dia inteiro, com bateria de longuíssima duração para trabalho remoto sem tomada.',
 'Tela 14" Full HD, 16GB RAM, 500GB SSD, até 14 horas de bateria, vem com Pop!_OS pré-instalado e otimizado.'),

('Dell XPS 13 Developer Edition - Core i7, Ubuntu', 'Notebook para programação com Linux nativo',
 'Versão certificada para desenvolvedores, com drivers e componentes totalmente compatíveis e testados em ambiente Linux.',
 'Tela 13.4" FHD+, 16GB RAM, 512GB SSD, chassi em alumínio usinado, certificação Project Sputnik da Dell para Ubuntu.'),

('MacBook Pro 14" - Chip M3 Pro', 'Notebook para desenvolvimento mobile iOS',
 'Essencial para quem desenvolve aplicativos iOS e macOS, com Xcode rodando de forma extremamente fluida e simulador de iPhone rápido.',
 'Chip M3 Pro com CPU de 11 núcleos e GPU de 14 núcleos, 18GB de memória unificada, 512GB SSD, tela Liquid Retina XDR de 14.2".'),

('Lenovo ThinkPad P1 - Core i7, RTX A2000', 'Notebook para DevOps e infraestrutura',
 'Combina robustez ThinkPad com placa de vídeo profissional, ideal para rodar múltiplas máquinas virtuais e containers simultaneamente.',
 'Tela 16" WQUXGA, 32GB RAM, 1TB SSD, certificação MIL-STD-810H, suporte a até 96GB de RAM para workloads pesados de virtualização.'),

('HP ZBook Firefly 14 - Core i7, Linux', 'Notebook para engenharia de software embarcado',
 'Workstation compacta homologada para ferramentas de desenvolvimento embarcado e compiladores cruzados de baixo nível.',
 'Tela 14" Full HD, 32GB RAM, 1TB SSD, certificação ISV para softwares de engenharia, portas USB-C com Thunderbolt 4.'),

('Asus ExpertBook B9 - Core i7', 'Notebook leve para desenvolvimento full stack',
 'Extremamente leve para levar entre reuniões e sessões de pair programming, sem abrir mão de desempenho para IDEs pesadas.',
 'Pesa apenas 995g. Tela 14" Full HD, 16GB RAM, 1TB SSD, bateria de longa duração, chassi em fibra de carbono militar.'),

('Acer Swift Go 14 - Core i7, Intel Evo', 'Notebook para desenvolvimento web',
 'Boa opção intermediária para quem programa em VS Code, roda containers Docker leves e navega com várias abas abertas.',
 'Tela 14" 2.8K OLED 90Hz, 16GB RAM, 512GB SSD, certificação Intel Evo com resposta rápida ao despertar.'),

('Huawei MateBook X Pro - Core i7', 'Notebook para programação e produtividade',
 'Design premium com tela imersiva, ótimo para quem programa e também documenta e apresenta o próprio trabalho.',
 'Tela 14.2" 3.1K touch 90Hz, 16GB RAM, 1TB SSD, chassi em liga de alumínio unibody, teclado retroiluminado silencioso.'),

('Chuwi CoreBook X - Core i5, Linux', 'Notebook econômico para programação',
 'Opção acessível para estudantes de programação que precisam rodar IDEs, terminais e ambientes de desenvolvimento básicos.',
 'Tela 14.1" Full HD, 12GB RAM, 512GB SSD, compatível com principais distribuições Linux, ótimo custo-benefício para iniciantes.'),

-- 21 a 30: Criadores de conteúdo (vídeo, música, design, foto, motion, streaming)
('Apple MacBook Pro 16" - Chip M3 Max 128GB', 'Notebook para edição de vídeo 8K',
 'O topo de linha para editores profissionais que trabalham com timelines pesadas de vídeo 8K RAW no DaVinci Resolve ou Final Cut Pro.',
 'Chip M3 Max com GPU de 40 núcleos, 128GB de memória unificada, 2TB SSD, tela Liquid Retina XDR com brilho de pico de 1600 nits.'),

('ASUS ProArt Studiobook 16 - Core i9, RTX 4080', 'Notebook para produção musical e áudio',
 'Certificado para softwares de áudio profissional, com calibração de cor de fábrica útil também para trilhas sonoras de vídeo.',
 'Tela 16" 4K OLED calibrada, 32GB RAM, 2TB SSD, ASUS Dial físico para ajuste fino em softwares criativos, alto-falantes Harman Kardon.'),

('Dell Precision 5680 - Core i9, RTX 4000 Ada', 'Notebook para design gráfico profissional',
 'Workstation móvel com certificação para Adobe Creative Cloud completo e softwares de ilustração vetorial pesada.',
 'Tela 16" UHD+ touch, 64GB RAM ECC, 2TB SSD, cores calibradas Pantone, placa de vídeo profissional NVIDIA RTX Ada Generation.'),

('Microsoft Surface Laptop Studio 2 - Core i7, RTX 4060', 'Notebook para fotografia e retoque',
 'Formato conversível único que permite usar caneta digital diretamente sobre a tela dobrada, ideal para retoque fotográfico detalhado.',
 'Tela 14.4" PixelSense Flow touch 120Hz, 32GB RAM, 1TB SSD, suporte a Surface Slim Pen 2 com resposta tátil.'),

('Lenovo Legion Pro 7i - Core i9, RTX 4090', 'Notebook para motion design e 3D',
 'Potência de sobra para renderização em After Effects, Cinema 4D e Blender sem depender de render farm externa.',
 'Tela 16" WQXGA 240Hz, 32GB RAM DDR5, 2TB SSD, sistema de refrigeração Legion Coldfront 5.0 com câmara de vapor.'),

('Apple MacBook Air 15" - Chip M3', 'Notebook para streaming e criação de conteúdo leve',
 'Silencioso e leve o suficiente para gravar podcasts e vídeos curtos sem captar ruído de ventoinha, com boa duração de bateria.',
 'Tela Liquid Retina de 15.3", chip M3, 16GB de memória unificada, 512GB SSD, sistema de som com six-speaker.'),

('MSI Creator Z16 - Core i9, RTX 4070', 'Notebook para produção de conteúdo em vídeo',
 'Voltado para criadores de conteúdo do YouTube e redes sociais que precisam editar, exportar e fazer upload rapidamente.',
 'Tela 16" QHD+ touch calibrada True Pixel, 32GB RAM, 1TB SSD, porta Thunderbolt 4 para transferência rápida de arquivos RAW.'),

('Gigabyte AERO 16 - Core i7, RTX 4060', 'Notebook para design de interiores e renderização',
 'Cores precisas e placa de vídeo dedicada tornam esse notebook uma boa escolha para renderizações em SketchUp e Lumion.',
 'Tela 16" UHD+ com validação Pantone, 32GB RAM, 1TB SSD, teclado retroiluminado com sensor de luz ambiente.'),

('Samsung Galaxy Book4 Ultra - Core i9, RTX 4070', 'Notebook para criadores multimídia',
 'Versátil para fotografia, vídeo e ilustração digital, com boa integração ao ecossistema de tablets Samsung para desenho.',
 'Tela 16" Dynamic AMOLED 2X 3K 120Hz, 32GB RAM, 1TB SSD, S Pen compatível, chassi em armor aluminum.'),

('LG Gram Pro 16 - Core i7, RTX 4050', 'Notebook leve para criadores em trânsito',
 'Um dos poucos notebooks com placa de vídeo dedicada que ainda é leve o suficiente para trabalhar em campo, longe do estúdio.',
 'Pesa apenas 1.65kg. Tela 16" WQXGA, 16GB RAM, 1TB SSD, bateria de longa duração mesmo com GPU dedicada ativa.'),

-- 31 a 40: Corporativo & Executivo
('Lenovo ThinkPad X13 - Core i7', 'Notebook corporativo ultraleve',
 'Focado em segurança de dados corporativos, com leitor biométrico e chip de segurança dedicado para ambientes empresariais.',
 'Tela 13.3" WUXGA, 16GB RAM, 512GB SSD, chip TPM 2.0, leitor de digitais e reconhecimento facial IR, certificação MIL-STD-810H.'),

('HP EliteBook 840 G11 - Core i7', 'Notebook corporativo com foco em segurança',
 'Um dos notebooks mais protegidos do mercado corporativo, com câmera com shutter físico e proteção contra visualização lateral.',
 'Tela 14" WUXGA com HP Sure View integrado, 16GB RAM, 512GB SSD, HP Wolf Security embarcado, webcam com shutter mecânico.'),

('Dell Latitude 9440 2-em-1 - Core i7', 'Notebook corporativo conversível',
 'Formato conversível pensado para executivos que alternam entre reuniões, apresentações e anotações à mão durante o dia.',
 'Tela 14" FHD+ touch conversível, 16GB RAM, 512GB SSD, design sem ventoinha em parte da linha, carregamento rápido ExpressCharge.'),

('ASUS ExpertBook B5 Flip - Core i7', 'Notebook corporativo com tela grande para produtividade',
 'Boa opção para quem trabalha com múltiplas planilhas e janelas abertas simultaneamente, sem precisar de um segundo monitor.',
 'Tela 16" WUXGA touch, 16GB RAM, 1TB SSD, dobradiça 360 graus, teclado numérico integrado.'),

('Lenovo ThinkPad L14 - Core i5', 'Notebook corporativo para contabilidade e finanças',
 'Teclado numérico dedicado facilita a digitação de planilhas financeiras extensas, com boa durabilidade para uso diário intenso.',
 'Tela 14" Full HD, 8GB RAM (expansível), 256GB SSD, teclado numérico completo, certificação de durabilidade militar.'),

('Microsoft Surface Laptop 6 - Core i7', 'Notebook executivo premium com Windows integrado',
 'Integração nativa com Microsoft 365 e Teams, com design minimalista adequado para apresentações em reuniões de negócios.',
 'Tela PixelSense 13.8" touch, 16GB RAM, 512GB SSD, acabamento premium em alumínio, câmera com Windows Hello.'),

('HP Dragonfly Pro - Core i7', 'Notebook executivo ultraportátil',
 'Extremamente leve e silencioso, projetado para quem viaja constantemente a trabalho e precisa de conforto no teclado.',
 'Pesa 1.33kg. Tela 13.5" 3:2, 16GB RAM, 512GB SSD, teclado premium com curso confortável, carregador USB-C compacto.'),

('Dell Latitude 7350 Detachable - Core i7', 'Notebook corporativo destacável',
 'Formato tablet destacável para vendedores externos e equipes de campo que precisam de mobilidade máxima em reuniões.',
 'Tela 13.4" 3K touch destacável, 16GB RAM, 512GB SSD, teclado magnético incluso, suporte a caneta digital.'),

('Lenovo ThinkPad T14s - Core i7', 'Notebook corporativo para segurança de dados',
 'Referência em segurança empresarial, com criptografia de disco por hardware e gerenciamento remoto para equipes de TI.',
 'Tela 14" WUXGA, 32GB RAM, 1TB SSD, suporte a Intel vPro, criptografia de hardware, ThinkShield Security.'),

('ASUS ExpertBook B7 Flip - Core i9', 'Notebook executivo de alta performance',
 'Para executivos que também rodam análises pesadas em Excel/Power BI, unindo portabilidade e processamento robusto.',
 'Tela 14" 2.8K OLED touch, 32GB RAM, 1TB SSD, chassi em liga de magnésio-lítio, dobradiça 360 graus reforçada.'),

-- 41 a 50: Estudantes & Custo-benefício
('Acer Chromebook Spin 514', 'Chromebook para estudantes',
 'Simples, rápido para ligar e usar, ideal para tarefas escolares baseadas em Google Docs, Classroom e navegação web.',
 'Tela 14" Full HD touch conversível, 8GB RAM, 128GB de armazenamento, ChromeOS, bateria de até 10 horas.'),

('Samsung Galaxy Book Go - Snapdragon 7c', 'Notebook básico para estudantes',
 'Opção enxuta e barata para estudantes que precisam basicamente de navegação, videochamadas e edição de texto.',
 'Tela 14" Full HD, 4GB RAM, 128GB eMMC, processador ARM Snapdragon com boa eficiência de bateria, sempre conectado via LTE opcional.'),

('Positivo Motion C4128B - Celeron', 'Notebook econômico escolar',
 'Notebook nacional voltado para o público estudantil de baixa renda, com foco em tarefas básicas de estudo remoto.',
 'Tela 14" HD, 4GB RAM, 128GB eMMC, leve com 1.3kg, ideal para uso com plataformas de ensino a distância.'),

('Multilaser Legacy Book - Celeron', 'Notebook básico custo-benefício',
 'Uma das opções mais acessíveis do mercado nacional, atendendo bem tarefas simples do dia a dia escolar e doméstico.',
 'Tela 14.1" HD, 4GB RAM, 64GB eMMC (expansível via SSD M.2), Windows 11 em modo S, bateria de 4 células.'),

('ASUS Vivobook Go 15 - Celeron', 'Notebook para universitários',
 'Boa opção de entrada para universitários que precisam de algo leve para levar ao campus e usar para trabalhos acadêmicos.',
 'Tela 15.6" Full HD, 8GB RAM, 256GB SSD, peso de 1.7kg, bateria de longa duração para um dia inteiro de aulas.'),

('Lenovo IdeaPad Slim 3 - Core i3', 'Notebook custo-benefício para estudos',
 'Processador Intel de entrada com desempenho suficiente para pesquisas escolares, editor de texto e streaming de aulas.',
 'Tela 15.6" Full HD antirreflexo, 8GB RAM, 256GB SSD, teclado numérico, Rapid Charge para carga rápida de 80% em 1 hora.'),

('HP 15 - Ryzen 3', 'Notebook doméstico para home office leve',
 'Voltado para tarefas leves de escritório em casa, como e-mails, planilhas simples e videochamadas do dia a dia.',
 'Tela 15.6" Full HD, 8GB RAM, 512GB SSD, webcam HP True Vision com correção de temperatura de cor.'),

('Acer Aspire Vero - Core i5', 'Notebook sustentável para estudantes',
 'Construído com plástico reciclado pós-consumo, voltado para estudantes e famílias preocupadas com impacto ambiental.',
 'Tela 15.6" Full HD, 8GB RAM, 512GB SSD, chassi em PCR (plástico reciclado) certificado, embalagem 100% reciclável.'),

('Xiaomi RedmiBook 15 - Core i5', 'Notebook custo-benefício para estudos e trabalho',
 'Boa relação entre preço e acabamento premium, com tela de qualidade acima da média para a faixa de preço.',
 'Tela 15.6" Full HD antirreflexo, 16GB RAM, 512GB SSD, chassi em liga metálica, leitor de digitais integrado.'),

('Realme Book Prime - Core i5', 'Notebook fino para estudantes universitários',
 'Design fino e moderno para universitários que querem algo bonito sem pagar preço de notebook premium.',
 'Tela 14" 2K, 8GB RAM, 512GB SSD, chassi unibody de alumínio, carregamento rápido de 65W via USB-C.'),

-- 51 a 60: Nicho e Especiais
('Dell Precision 7780 - Core i9, RTX 5000 Ada', 'Notebook workstation certificada para CAD',
 'Certificado por fabricantes de software CAD/CAM para engenharia mecânica, com validação ISV para AutoCAD, SolidWorks e Revit.',
 'Tela 17.3" UHD+, 128GB RAM ECC, 4TB SSD, placa de vídeo profissional NVIDIA RTX Ada, certificação ISV completa.'),

('Panasonic Toughbook 55', 'Notebook robusto militar',
 'Resistente a quedas, poeira, água e temperaturas extremas, usado em campo por equipes de manutenção e forças de segurança.',
 'Tela 14" sunlight-readable, 16GB RAM, 512GB SSD, certificação MIL-STD-810H e IP53, chassi semi-rugged intercambiável.'),

('Lenovo ThinkPad X13s - Snapdragon 8cx Gen 3, 5G', 'Notebook com conectividade 5G para trabalho remoto',
 'Sempre conectado à internet via rede móvel 5G, ideal para profissionais que trabalham em campo sem depender de Wi-Fi.',
 'Tela 13" 3K, 16GB RAM, 512GB SSD, modem 5G integrado, processador ARM com altíssima eficiência energética.'),

('ASUS Zenbook 17 Fold - Core i7', 'Notebook com tela dobrável',
 'Tela dobrável inovadora que se transforma de notebook compacto em um monitor grande de 17 polegadas.',
 'Tela OLED dobrável 17.3"/12.5", 16GB RAM, 1TB SSD, teclado Bluetooth destacável, dobradiça de titânio líquido.'),

('NVIDIA-powered Lambda Tensorbook - Core i9, RTX 4090', 'Notebook para IA e Machine Learning local',
 'Voltado para cientistas de dados que precisam treinar e testar modelos de machine learning localmente antes de escalar para a nuvem.',
 'Tela 16" QHD+ 240Hz, 64GB RAM, 2TB SSD, ambiente Linux com CUDA, PyTorch e TensorFlow pré-instalados e otimizados.'),

('HP ZBook Fury 17 G10 - Core i9, RTX 5000 Ada', 'Notebook workstation para engenharia pesada',
 'Voltado para simulações de engenharia estrutural, fluidodinâmica computacional e outras cargas de trabalho científicas pesadas.',
 'Tela 17.3" UHD, 128GB RAM, 4TB SSD RAID, certificação ISV para ANSYS e MATLAB, sistema de refrigeração de alta capacidade.'),

('GPD Win Max 2 - Core i7', 'Mini notebook portátil para nicho gamer retro',
 'Formato compacto tipo mini-notebook, popular entre entusiastas de emulação retro e jogos indie em qualquer lugar.',
 'Tela 10.1" 2.5K 120Hz, 32GB RAM, 2TB SSD, controles de jogo integrados ao chassi, extremamente compacto e portátil.'),

('One-Netbook OneXPlayer - Core i7', 'Notebook ultracompacto para nicho técnico',
 'Nicho de entusiastas que querem um PC completo do tamanho de um tablet grande, útil para diagnósticos técnicos em campo.',
 'Tela 8.4" 2.5K, 16GB RAM, 1TB SSD, portas completas apesar do tamanho reduzido, bateria de longa duração.'),

('Dynabook Portégé X30L - Core i7', 'Notebook ultraleve corporativo japonês',
 'Tradição japonesa em notebooks corporativos, com foco extremo em leveza e confiabilidade para uso empresarial diário.',
 'Pesa apenas 870g. Tela 13.3" Full HD, 16GB RAM, 512GB SSD, chassi em magnésio, um dos notebooks mais leves do mundo.'),

('VAIO SX14 - Core i7', 'Notebook premium híbrido de trabalho',
 'Marca japonesa com forte apelo de qualidade de construção, voltada para profissionais que valorizam acabamento refinado.',
 'Tela 14" Full HD, 16GB RAM, 512GB SSD, chassi em fibra de carbono e alumínio, teclado premium com curso profundo.');
