import { render, screen, within } from '@testing-library/react';
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
