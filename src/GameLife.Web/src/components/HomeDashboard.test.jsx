import { fireEvent, render, screen, within } from '@testing-library/react';
import { vi } from 'vitest';
import HomeDashboard from './HomeDashboard.jsx';

const dashboard = {
  resumo: {
    tempoNaPlataformaAnos: 6,
    jogosZerados: 82,
    horasZeradas: 31.5,
    totalJogos: 2,
    totalColecoes: 3,
    conquistasPossiveis: 4299,
    conquistasObtidas: 2971,
    percentualConquistas: 69.11
  },
  bibliotecaPorPlataforma: [
    { plataforma: 'PSN', total: 1 },
    { plataforma: 'STEAM', total: 1 }
  ],
  zeradosPorPlataforma: [
    { plataforma: 'PSN', total: 32 },
    { plataforma: 'STEAM', total: 50 }
  ],
  zeradosPorAno: [
    { ano: 2024, total: 40 },
    { ano: 2025, total: 42 }
  ],
  jogosRecentes: [],
  conquistas: []
};

const perfil = { nome: 'Lucas', fotoDataUrl: null, exibirJogosRecentes: true };

test('apresenta o resumo vivo com plataformas e anos', () => {
  render(<HomeDashboard dashboard={dashboard} perfil={perfil} />);
  expect(within(screen.getByText('Tempo na plataforma').closest('article')).getByText('6 anos')).toBeInTheDocument();
  expect(within(screen.getByText('Jogos zerados').closest('article')).getByText('82')).toBeInTheDocument();
  expect(within(screen.getByText('Horas de zerados').closest('article')).getByText('31:30:00')).toBeInTheDocument();
  expect(within(screen.getByText('Jogos na biblioteca').closest('article')).getByText('2')).toBeInTheDocument();
  expect(within(screen.getByText('Conquistas possíveis').closest('article')).getByText('4299')).toBeInTheDocument();
  expect(within(screen.getByText('Conquistas obtidas').closest('article')).getByText('2971')).toBeInTheDocument();
  expect(within(screen.getByText('Percentual de conquistas').closest('article')).getByText('69,11%')).toBeInTheDocument();
  expect(screen.getByRole('heading', { name: 'Jogos zerados por plataforma' })).toBeInTheDocument();
  expect(screen.getByRole('heading', { name: 'Jogos na biblioteca por plataforma' })).toBeInTheDocument();
  expect(screen.getAllByText('PSN')).toHaveLength(2);
  expect(screen.getAllByText('STEAM')).toHaveLength(2);
  expect(screen.getByRole('heading', { name: 'Total zerados por ano' })).toBeInTheDocument();
  const gradeAnual = screen.getByRole('heading', { name: 'Total zerados por ano' }).parentElement.querySelector('.year-grid');
  expect(gradeAnual).toHaveStyle({ gridTemplateColumns: 'repeat(auto-fit, minmax(70px, 1fr))' });
  expect(screen.getByText('2024')).toBeInTheDocument();
  expect(screen.getByText('2025')).toBeInTheDocument();
});

test('mostra alerta na tela inicial para jogo na biblioteca com preço monitorado e ações', () => {
  const onRemoverDesejo = vi.fn();
  const onContinuarMonitorando = vi.fn();

  render(
    <HomeDashboard
      dashboard={{
        ...dashboard,
        alertasListaDesejos: [
          {
            desejoId: 9,
            jogoId: 42,
            titulo: 'Hollow Knight',
            lojasMonitoradas: ['NUUVEM', 'STEAM']
          }
        ]
      }}
      perfil={perfil}
      onRemoverDesejo={onRemoverDesejo}
      onContinuarMonitorando={onContinuarMonitorando}
    />
  );

  const regiaoAlertas = screen.getByRole('region', { name: 'Alertas da Lista de Desejos' });
  expect(within(regiaoAlertas).getByRole('heading', { name: 'Hollow Knight' })).toBeInTheDocument();
  expect(within(regiaoAlertas).getByText('NUUVEM')).toBeInTheDocument();
  expect(within(regiaoAlertas).getByText('STEAM')).toBeInTheDocument();

  fireEvent.click(within(regiaoAlertas).getByRole('button', { name: 'Continuar monitorando' }));
  expect(onContinuarMonitorando).toHaveBeenCalledWith(expect.objectContaining({ desejoId: 9 }));

  fireEvent.click(within(regiaoAlertas).getByRole('button', { name: 'Remover da Lista de Desejos' }));
  expect(onRemoverDesejo).toHaveBeenCalledWith(expect.objectContaining({ desejoId: 9 }));
});

test.each([[12, '12 dias'], [1, '1 dia'], [0, 'hoje']])('exibe a saída do catálogo em %d dias', (diasRestantes, prazo) => {
  render(<HomeDashboard perfil={{}} dashboard={{ alertasSaidaCatalogo: [{
    jogoId: 7, titulo: 'Hi-Fi Rush', servico: 'Game Pass',
    dataEncerramento: '2026-12-01T00:00:00', diasRestantes
  }] }} />);
  const regiao = within(screen.getByRole('region', { name: 'Saindo do catálogo' }));
  expect(regiao.getByText('Hi-Fi Rush')).toBeInTheDocument();
  expect(regiao.getByText('Game Pass · 01/12/2026')).toBeInTheDocument();
  expect(regiao.getByText(prazo)).toBeInTheDocument();
});

test('omite a região quando não há saídas próximas', () => {
  render(<HomeDashboard perfil={{}} dashboard={{ alertasSaidaCatalogo: [] }} />);
  expect(screen.queryByRole('region', { name: 'Saindo do catálogo' })).not.toBeInTheDocument();
});
