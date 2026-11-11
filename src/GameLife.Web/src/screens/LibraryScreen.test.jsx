import { fireEvent, render, screen, within } from '@testing-library/react';
import { vi } from 'vitest';
import LibraryScreen from './LibraryScreen.jsx';

const items = [
  {
    jogoId: 1,
    titulo: 'Graveyard Keeper',
    anoLancamento: 2018,
    plataformas: [
      { posseId: 11, nome: 'STEAM', midiaFisica: false, acompanhamento: null },
      {
        posseId: 12,
        nome: 'XBOX',
        midiaFisica: true,
        acompanhamento: {
          status: 'Zerado',
          tempoEmHoras: 22,
          conquistasPossiveis: 12,
          conquistasObtidas: 10
        }
      }
    ]
  },
  {
    jogoId: 2,
    titulo: 'Éclipse',
    anoLancamento: null,
    plataformas: [
      {
        posseId: 21,
        nome: 'PC',
        midiaFisica: false,
        acompanhamento: {
          status: 'NaoTerminado',
          tempoEmHoras: 4,
          conquistasPossiveis: 10,
          conquistasObtidas: 3
        }
      }
    ]
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

test('mostra mídia e progresso independentes por plataforma', () => {
  render(<LibraryScreen items={[items[0]]} loading={false} />);

  const steam = screen.getByRole('group', { name: 'STEAM' });
  expect(within(steam).getByText('Digital')).toBeInTheDocument();
  expect(within(steam).getByText('Sem acompanhamento')).toBeInTheDocument();

  const xbox = screen.getByRole('group', { name: 'XBOX' });
  expect(within(xbox).getByText('Mídia física')).toBeInTheDocument();
  expect(within(xbox).getByText('22 h')).toBeInTheDocument();
  expect(within(xbox).getByText('10 / 12')).toBeInTheDocument();
  expect(within(xbox).getByText('Zerado')).toBeInTheDocument();
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
