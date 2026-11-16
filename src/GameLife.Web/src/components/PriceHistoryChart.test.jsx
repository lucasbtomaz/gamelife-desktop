import { fireEvent, render, screen, within } from '@testing-library/react';
import PriceHistoryChart from './PriceHistoryChart.jsx';

const registros = [
  { id: 3, canalId: 1, canalNome: 'STEAM', precoAtual: 53.06, consultadoEm: '2026-11-12T17:50:51Z' },
  { id: 1, canalId: 1, canalNome: 'STEAM', precoAtual: 79.9, consultadoEm: '2026-01-10T12:00:00Z' },
  { id: 4, canalId: 2, canalNome: 'PlayStation', precoAtual: 53.06, consultadoEm: '2026-12-01T09:00:00Z' },
  { id: 2, canalId: 2, canalNome: 'PlayStation', precoAtual: 95.94, consultadoEm: '2026-02-20T12:00:00Z' }
];

test('desenha uma série por plataforma com todos os registros em ordem cronológica', () => {
  render(<PriceHistoryChart registros={registros} tituloJogo="Slime Rancher 2" />);

  const grafico = screen.getByRole('group', { name: 'Histórico de preços de Slime Rancher 2' });
  expect(within(grafico).getByLabelText('Evolução de preços na STEAM')).toBeInTheDocument();
  expect(within(grafico).getByLabelText('Evolução de preços na PlayStation')).toBeInTheDocument();

  const pontos = within(grafico).getAllByRole('img');
  expect(pontos).toHaveLength(4);
  expect(pontos.map((ponto) => ponto.getAttribute('aria-label'))).toEqual([
    expect.stringMatching(/STEAM, R\$\s79,90/),
    expect.stringMatching(/STEAM, R\$\s53,06/),
    expect.stringMatching(/PlayStation, R\$\s95,94/),
    expect.stringMatching(/PlayStation, R\$\s53,06/)
  ]);
  expect(screen.getByLabelText('Legenda do histórico de preços')).toHaveTextContent('STEAM');
  expect(screen.getByLabelText('Legenda do histórico de preços')).toHaveTextContent('PlayStation');
});

test('mostra os detalhes do ponto por mouse e teclado e destaca um único menor preço', () => {
  render(<PriceHistoryChart registros={registros} tituloJogo="Slime Rancher 2" />);

  const pontos = screen.getAllByRole('img', { name: /R\$/ });
  fireEvent.mouseEnter(pontos[0]);
  expect(screen.getByRole('status')).toHaveTextContent('STEAM');
  expect(screen.getByRole('status')).toHaveTextContent('R$ 79,90');
  expect(screen.getByRole('status')).toHaveTextContent('10/01/2026');

  fireEvent.mouseLeave(pontos[0]);
  fireEvent.focus(pontos[1]);
  expect(screen.getByRole('status')).toHaveTextContent('12/11/2026');
  expect(screen.getAllByRole('img', { name: /Menor preço histórico/ })).toHaveLength(1);
  expect(screen.getByRole('img', { name: /Menor preço histórico/ })).toHaveAccessibleName(/STEAM/);
});

test('mantém pontos únicos e domínios constantes com coordenadas válidas', () => {
  const { container } = render(
    <PriceHistoryChart
      tituloJogo="Mesmo preço"
      registros={[
        { id: 1, canalId: 1, canalNome: 'STEAM', precoAtual: 50, consultadoEm: '2026-01-01T12:00:00Z' },
        { id: 2, canalId: 2, canalNome: 'PlayStation', precoAtual: 50, consultadoEm: '2026-01-01T12:00:00Z' }
      ]}
    />
  );

  expect(screen.getAllByRole('img', { name: /R\$\s50,00/ })).toHaveLength(2);
  expect(screen.queryByLabelText('Evolução de preços na STEAM')).not.toBeInTheDocument();
  expect(screen.getAllByText('01/01/2026')).toHaveLength(1);
  expect(container.innerHTML).not.toMatch(/NaN|Infinity/);
});

test('mantém a cor de cada plataforma quando a API muda a ordem dos registros', () => {
  const { rerender } = render(<PriceHistoryChart registros={registros} tituloJogo="Cores estáveis" />);
  const corSteam = screen.getByText('STEAM').querySelector('i').style.backgroundColor;
  const corPlayStation = screen.getByText('PlayStation').querySelector('i').style.backgroundColor;

  rerender(<PriceHistoryChart registros={[...registros].reverse()} tituloJogo="Cores estáveis" />);

  expect(screen.getByText('STEAM').querySelector('i')).toHaveStyle({ backgroundColor: corSteam });
  expect(screen.getByText('PlayStation').querySelector('i')).toHaveStyle({ backgroundColor: corPlayStation });
});

test('informa quando ainda não existe histórico', () => {
  render(<PriceHistoryChart registros={[]} tituloJogo="Sem histórico" />);

  expect(screen.getByText('Nenhum histórico de preço cadastrado.')).toBeInTheDocument();
  expect(screen.queryByRole('group')).not.toBeInTheDocument();
});
