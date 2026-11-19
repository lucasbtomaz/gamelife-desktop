import { fireEvent, render, screen, within } from '@testing-library/react';
import { vi } from 'vitest';
import LibraryScreen from './LibraryScreen.jsx';

const items = [
  {
    jogoId: 1,
    titulo: 'Graveyard Keeper',
    anoLancamento: 2018,
    plataformas: [
      { posseId: 11, nome: 'STEAM', compraDigital: true, midiaFisica: false, porAssinatura: false, acompanhamento: null },
      {
        posseId: 12,
        nome: 'XBOX',
        compraDigital: false,
        midiaFisica: true,
        porAssinatura: false,
        acompanhamento: {
          status: 'Zerado',
          tempoEmHoras: 22,
          conquistasPossiveis: 12,
          conquistasObtidas: 10
        }
      }
    ],
    assinaturas: []
  },
  {
    jogoId: 2,
    titulo: 'Éclipse',
    anoLancamento: null,
    plataformas: [
      {
        posseId: 21,
        nome: 'PC',
        compraDigital: true,
        midiaFisica: false,
        porAssinatura: false,
        acompanhamento: {
          status: 'NaoTerminado',
          tempoEmHoras: 4,
          conquistasPossiveis: 10,
          conquistasObtidas: 3
        }
      }
    ],
    assinaturas: []
  }
];

test('exibe uma ficha para jogo com duas plataformas', () => {
  render(<LibraryScreen items={items} loading={false} />);
  const cards = screen.getAllByRole('article');
  expect(cards).toHaveLength(2);
  const graveyard = cards.find((card) => within(card).queryByText('Graveyard Keeper'));
  expect(graveyard).toBeDefined();
  expect(within(graveyard).getByText('STEAM')).toBeInTheDocument();
  expect(within(graveyard).getByText('XBOX')).toBeInTheDocument();
  expect(within(graveyard).getAllByRole('heading', { name: 'Graveyard Keeper' })).toHaveLength(1);
});

test('mostra formas de acesso e progresso independentes por plataforma', () => {
  render(<LibraryScreen items={[items[0]]} loading={false} />);
  const steam = screen.getByRole('group', { name: 'STEAM' });
  expect(within(steam).getByText('Compra digital')).toBeInTheDocument();
  expect(within(steam).getByText('Sem acompanhamento')).toBeInTheDocument();
  const xbox = screen.getByRole('group', { name: 'XBOX' });
  expect(within(xbox).getByText('Mídia física')).toBeInTheDocument();
  expect(within(xbox).getByText('22 h')).toBeInTheDocument();
  expect(within(xbox).getByText('10 / 12')).toBeInTheDocument();
  expect(within(xbox).getByText('Zerado')).toBeInTheDocument();
});

test('exibe badges compactas de compra e assinatura simultâneas e jogo só por assinatura', () => {
  const comAssinatura = [
    {
      jogoId: 3,
      titulo: 'Lies of P',
      anoLancamento: 2023,
      plataformas: [
        { posseId: 31, nome: 'XBOX', compraDigital: true, midiaFisica: false, porAssinatura: true, acompanhamento: null }
      ],
      assinaturas: [
        { id: 1, jogoId: 3, servico: 'Game Pass', situacao: 'Disponível' },
        { id: 2, jogoId: 3, servico: 'PlayStation Plus', situacao: 'Encerrado' }
      ]
    },
    {
      jogoId: 4,
      titulo: 'Sea of Stars',
      anoLancamento: 2023,
      plataformas: [
        { posseId: 41, nome: 'PlayStation 5', compraDigital: false, midiaFisica: false, porAssinatura: true, acompanhamento: null }
      ],
      assinaturas: [
        { id: 3, jogoId: 4, servico: 'PlayStation Plus', situacao: 'Disponível' }
      ]
    }
  ];

  render(<LibraryScreen items={comAssinatura} loading={false} />);
  const xbox = screen.getByRole('group', { name: 'XBOX' });
  expect(within(xbox).getByText('Compra digital')).toBeInTheDocument();
  expect(within(xbox).getByText('Game Pass · Disponível')).toBeInTheDocument();
  expect(within(xbox).getByText('PlayStation Plus · Encerrado')).toBeInTheDocument();

  expect(screen.queryByRole('group', { name: 'PlayStation 5' })).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('Visão do acervo'), { target: { value: 'catalogo' } });
  expect(screen.queryByRole('group', { name: 'XBOX' })).not.toBeInTheDocument();
  expect(screen.getByRole('group', { name: 'PlayStation 5' })).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('Visão do acervo'), { target: { value: 'todos' } });
  expect(screen.getByRole('group', { name: 'XBOX' })).toBeInTheDocument();
  const ps5 = screen.getByRole('group', { name: 'PlayStation 5' });
  expect(within(ps5).queryByText('Compra digital')).not.toBeInTheDocument();
  expect(within(ps5).getByText('PlayStation Plus · Disponível')).toBeInTheDocument();
});

test('filtra a ficha quando qualquer plataforma corresponde', () => {
  render(<LibraryScreen items={items} loading={false} />);
  fireEvent.click(screen.getByRole('button', { name: 'XBOX' }));
  expect(screen.getByText('Graveyard Keeper')).toBeInTheDocument();
  expect(screen.queryByText('Éclipse')).not.toBeInTheDocument();
  expect(screen.getByRole('group', { name: 'XBOX' })).toBeInTheDocument();
  expect(screen.queryByRole('group', { name: 'STEAM' })).not.toBeInTheDocument();
});

test('filtro físico mantém somente plataformas físicas visíveis', () => {
  render(<LibraryScreen items={items} loading={false} />);
  fireEvent.click(screen.getByRole('checkbox', { name: 'Apenas mídia física' }));
  expect(screen.getByText('Graveyard Keeper')).toBeInTheDocument();
  expect(screen.getByRole('group', { name: 'XBOX' })).toBeInTheDocument();
  expect(screen.queryByRole('group', { name: 'STEAM' })).not.toBeInTheDocument();
  expect(screen.queryByText('Éclipse')).not.toBeInTheDocument();
});

test('busca continua ignorando caixa e acentos', () => {
  render(<LibraryScreen items={items} loading={false} />);
  fireEvent.change(screen.getByRole('searchbox', { name: 'Buscar jogos' }), { target: { value: 'ECLIPSE' } });
  expect(screen.getByText('Éclipse')).toBeInTheDocument();
  expect(screen.queryByText('Graveyard Keeper')).not.toBeInTheDocument();
});

test('aciona a edição da ficha única selecionada', () => {
  const onEdit = vi.fn();
  render(<LibraryScreen items={items} loading={false} onEdit={onEdit} />);
  fireEvent.click(screen.getByRole('button', { name: 'Editar Graveyard Keeper' }));
  expect(onEdit).toHaveBeenCalledTimes(1);
  expect(onEdit).toHaveBeenCalledWith(items[0]);
});

test.each([
  ['BloqueadoSemAssinatura', 'Assinatura inativa'],
  ['JogavelPorResgate', 'Jogável com a assinatura']
])('exibe o estado do resgate %s e oculta a plataforma de catálogo', (estadoAcesso, aviso) => {
  render(<LibraryScreen items={[{ jogoId: 8, titulo: 'Jogo misto', plataformas: [
    { posseId: 9, nome: 'PSN', porAssinatura: true, resgatadoDeAssinatura: true,
      servicoExigido: 'PlayStation Plus', estadoAcesso },
    { posseId: 10, nome: 'Xbox', porAssinatura: true }
  ] }]} loading={false} />);

  expect(screen.getByText('Resgatado · PlayStation Plus')).toBeInTheDocument();
  expect(screen.getByText(aviso)).toBeInTheDocument();
  expect(screen.queryByRole('group', { name: 'Xbox' })).not.toBeInTheDocument();
});

test('renderiza só uma página e ordena o acervo completo na lista compacta', () => {
  const jogos = Array.from({ length: 1001 }, (_, i) => ({
    jogoId: i + 1,
    titulo: `Jogo ${i + 1}`,
    plataformas: [{ posseId: i + 1, nome: 'PC', compraDigital: true }]
  }));
  render(<LibraryScreen items={jogos} loading={false} />);
  expect(screen.getByLabelText('Exibição do acervo')).toHaveValue('lista');
  expect(screen.getAllByRole('article')).toHaveLength(24);
  expect(screen.getByRole('heading', { name: 'Jogo 1', exact: true })).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Próxima página' }));
  expect(screen.getByRole('heading', { name: 'Jogo 25', exact: true })).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('Exibição do acervo'), { target: { value: 'cards' } });
  expect(screen.getAllByRole('article')).toHaveLength(24);
  expect(screen.getByRole('heading', { name: 'Jogo 25', exact: true })).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('Ordenar jogos'), { target: { value: 'titulo-desc' } });
  expect(screen.getByRole('heading', { name: 'Jogo 1001', exact: true })).toBeInTheDocument();
  fireEvent.change(screen.getByRole('searchbox', { name: 'Buscar jogos' }), { target: { value: 'Jogo 1000' } });
  expect(screen.getAllByRole('article')).toHaveLength(1);
  expect(screen.getByRole('heading', { name: 'Jogo 1000', exact: true })).toBeInTheDocument();
});
