import { fireEvent, render, screen } from '@testing-library/react';
import LibraryScreen from './LibraryScreen.jsx';

const items = [
  { id: 1, titulo: 'Éclipse', plataforma: 'PC', midiaFisica: false, anoLancamento: null, status: null, tempoEmHoras: null, conquistasPossiveis: null, conquistasObtidas: null },
  { id: 2, titulo: 'Celeste', plataforma: 'STEAM', midiaFisica: true, anoLancamento: 2018, status: 'Zerado', tempoEmHoras: 12.5, conquistasPossiveis: 30, conquistasObtidas: 24 },
  { id: 3, titulo: 'Cocoon', plataforma: 'PC', midiaFisica: true, anoLancamento: 2023, status: 'NaoTerminado', tempoEmHoras: 4, conquistasPossiveis: 10, conquistasObtidas: 3 }
];

test('exibe todos os jogos reais em cards sem filtro', () => {
  render(<LibraryScreen items={items} loading={false} />);

  expect(screen.getByText('Éclipse')).toBeInTheDocument();
  expect(screen.getByText('Celeste')).toBeInTheDocument();
  expect(screen.getByText('Cocoon')).toBeInTheDocument();
  expect(screen.getAllByRole('article')).toHaveLength(3);
});

test('combina busca, plataforma e mídia física', () => {
  render(<LibraryScreen items={items} loading={false} />);

  fireEvent.change(screen.getByRole('searchbox', { name: 'Buscar jogos' }), { target: { value: 'CO' } });
  fireEvent.click(screen.getByRole('button', { name: 'PC' }));
  fireEvent.click(screen.getByRole('checkbox', { name: 'Apenas mídia física' }));

  expect(screen.getByText('Cocoon')).toBeInTheDocument();
  expect(screen.queryByText('Éclipse')).not.toBeInTheDocument();
  expect(screen.queryByText('Celeste')).not.toBeInTheDocument();
});

test('busca ignora acentos e diferenças de caixa', () => {
  render(<LibraryScreen items={items} loading={false} />);

  fireEvent.change(screen.getByRole('searchbox', { name: 'Buscar jogos' }), { target: { value: 'eclipse' } });

  expect(screen.getByText('Éclipse')).toBeInTheDocument();
});

test('não inventa métricas ausentes', () => {
  render(<LibraryScreen items={[items[0]]} loading={false} />);

  expect(screen.getAllByText('Não informado')).toHaveLength(3);
  expect(screen.getByText('Sem acompanhamento')).toBeInTheDocument();
});

test('informa quando os filtros não encontram jogos', () => {
  render(<LibraryScreen items={items} loading={false} />);

  fireEvent.change(screen.getByRole('searchbox', { name: 'Buscar jogos' }), { target: { value: 'inexistente' } });

  expect(screen.getByText('Nenhum jogo encontrado com esses filtros.')).toBeInTheDocument();
});
