import { fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import PainelRetrospectiva from './PainelRetrospectiva.jsx';

const jogosJaneiro = [
  { id: 1, titulo: 'Elden Ring', plataforma: 'PlayStation 5', status: 'NaoTerminado', tempoEmHoras: 100, conquistasObtidas: 30, conquistasPossiveis: 42, dataStatus: '2027-01-02' },
  { id: 2, titulo: 'Hades II', plataforma: 'STEAM', status: 'Zerado', tempoEmHoras: 50, conquistasObtidas: 30, conquistasPossiveis: 35, dataStatus: '2027-01-04' },
  { id: 3, titulo: 'Celeste', plataforma: 'Nintendo Switch', status: 'Dropado', tempoEmHoras: 25, conquistasObtidas: 12, conquistasPossiveis: 30, dataStatus: '2027-01-06' }
];

function responderCom(dados) {
  global.fetch = vi.fn().mockResolvedValue({
    ok: true,
    json: async () => dados
  });
}

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: true });
  vi.setSystemTime(new Date('2027-01-15T12:00:00-03:00'));
  responderCom(jogosJaneiro);
});

afterEach(() => {
  vi.useRealTimers();
  vi.restoreAllMocks();
});

test('gera os períodos a partir da data atual', () => {
  render(<PainelRetrospectiva />);

  expect(screen.getByRole('heading', { name: /review do gamer/i })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Janeiro/2027 (Atual)' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Dezembro/2026' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Retrospectiva Anual 2027' })).toBeInTheDocument();
});

test('não exibe dados demonstrativos quando a API retorna uma lista vazia', async () => {
  responderCom([]);
  render(<PainelRetrospectiva />);

  expect(await screen.findByText('Nenhum jogo registrado neste período.')).toBeInTheDocument();
  expect(screen.queryByText('The Witcher 3: Wild Hunt')).not.toBeInTheDocument();
});

test('não reaproveita jogos de outro período quando o mês atual está vazio', async () => {
  responderCom([{ ...jogosJaneiro[0], titulo: 'Jogo de Dezembro', dataStatus: '2026-12-20' }]);
  render(<PainelRetrospectiva />);

  expect(await screen.findByText('Nenhum jogo registrado neste período.')).toBeInTheDocument();
  expect(screen.queryByText('Jogo de Dezembro')).not.toBeInTheDocument();
});

test('mostra erro da API sem substituir o conteúdo por dados fictícios', async () => {
  global.fetch = vi.fn().mockRejectedValue(new Error('falha de rede'));
  render(<PainelRetrospectiva />);

  expect(await screen.findByRole('alert')).toHaveTextContent('Não foi possível carregar a retrospectiva.');
  expect(screen.queryByText('The Witcher 3: Wild Hunt')).not.toBeInTheDocument();
});

test('ordena o ranking por horas e apresenta os detalhes de cada jogo', async () => {
  render(<PainelRetrospectiva />);

  const ranking = await screen.findByRole('region', { name: 'Jogos mais jogados no período' });
  const linhas = within(ranking).getAllByRole('article');

  expect(linhas).toHaveLength(3);
  expect(linhas[0]).toHaveAccessibleName('#1 Elden Ring');
  expect(linhas[0]).toHaveTextContent('Em andamento');
  expect(linhas[0]).toHaveTextContent('PlayStation 5');
  expect(linhas[0]).toHaveTextContent('30 / 42');
  expect(linhas[0]).toHaveTextContent('57,1% do tempo');
  expect(linhas[1]).toHaveAccessibleName('#2 Hades II');
  expect(linhas[2]).toHaveAccessibleName('#3 Celeste');
});

test('mantém o modo de retrospectiva anual disponível', async () => {
  render(<PainelRetrospectiva />);
  fireEvent.click(screen.getByRole('button', { name: 'Retrospectiva Anual' }));

  expect(await screen.findByText('Seu Ano no Mundo dos Games')).toBeInTheDocument();
});
